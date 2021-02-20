using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using InsideEM.Collections;

namespace InsideEM
{
    public partial struct EmbedMenu<UserT, ChannelT>
        where UserT: IUser 
        where ChannelT: ITextChannel
    {
        public delegate void EmbedMenuDel(ref EmbedMenu<UserT, ChannelT> EM);
        
        public struct EmbedMenuAct
        {
            public string Emoji, Name, Desc;

            public EmbedMenuDel Act;

            [MethodImpl(EMHelpers.InlineAndOptimize)]
            public EmbedMenuAct(string emoji, string name, string desc, EmbedMenuDel act)
            {
                Emoji = emoji;

                Name = name;

                Desc = desc;

                Act = act;
            }
        }
        
        private EmbedMenuDel InitAct;

        public string Title, Desc;
        
        public int CurrentEMIndex, CurrentPageNumber, Pages, MaxElemsPerPage;

        internal PooledList<EmbedMenu<UserT, ChannelT>> EMHistory;
        
        internal PooledList<EmbedMenuAct> Acts;
        
        internal UserT User;

        internal ChannelT Channel;

        [MethodImpl(EMHelpers.InlineAndOptimize)]
        public EmbedMenu(ref EmbedMenuAct ExecutedEMAct, ref EmbedMenu<UserT, ChannelT> PrevEM, string title, string desc)
        {
            InitAct = ExecutedEMAct.Act;
            
            EMHistory = PrevEM.EMHistory;

            Title = title;

            Desc = desc;

            Acts = PrevEM.Acts;

            CurrentEMIndex = unchecked(++PrevEM.CurrentEMIndex);
            
            //Page defaults
            
            CurrentPageNumber = 0;

            MaxElemsPerPage = 5;

            Pages = EMHelpers.DivideAndRoundUpFast(Acts.Count, MaxElemsPerPage);
            
            //User creds

            User = PrevEM.User;

            Channel = PrevEM.Channel;
            
            Unsafe.SkipInit(out CurrentMsg);
        }
    }

    public partial struct EmbedMenu<UserT, ChannelT>
    {
        private static readonly EmbedBuilder EMB;

        static EmbedMenu()
        {
            EMB = new EmbedBuilder();
        }
        
        private const string CrossEmoji = "❎";

        private const string BackEmoji = "🔙";

        private const string NavLeftEmoji = "⬅️";
        
        private const string NavRightEmoji = "➡️";

        private IUserMessage CurrentMsg;

        [MethodImpl(EMHelpers.InlineAndOptimize)]
        internal async Task Compile(DiscordSocketClient Client)
        {
            CurrentMsg = await Channel.SendMessageAsync(null, false, EMB.Build());
            
            //Setup reaction handler

            Client.ReactionAdded += OnReactionAdded;
        }

        [MethodImpl(EMHelpers.InlineAndOptimize)]
        private Task OnReactionAdded(Cacheable<IUserMessage, ulong> Cacheable, ISocketMessageChannel SocketMessageChannel, SocketReaction React)
        {
            var ReactName = React.Emote.Name;
            
            //Starting index would be the PageNumber * MaxElemsPerPage
            
            //E.x. Assuming that MaxElemsPerPage is 5, page number of 0 would mean we start from Index 0 * 5 = 0...
            //Page number of 1 would mean we start from 1 * 5 = 5
            //Such is true since Array Indexes are zero-based

            Acts.AsSpan(CurrentPageNumber, MaxElemsPerPage, out var PageActs);

            foreach (var Act in PageActs)
            {
                if (Act.Emoji == ReactName)
                {
                    Act.Act(ref this);

                    return Task.CompletedTask;
                }
            }
            
            if (ReactName == BackEmoji)
            {
                Back();
                
                return Task.CompletedTask;
            }
            
            if (ReactName == CrossEmoji)
            {
                ssss
                
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }

        [MethodImpl(EMHelpers.InlineAndOptimize)]
        private void Back()
        {
            if (CurrentEMIndex == 0)
            {
                return;
            }

            ref var PrevEM = ref EMHistory[unchecked(CurrentEMIndex - 1)];

            PrevEM.Acts.Clear();
            
            PrevEM.InitAct(ref PrevEM);
        }
    }
}