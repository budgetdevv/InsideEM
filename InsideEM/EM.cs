using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using InsideEM.Collections;
using InsideEM.Memory;

namespace InsideEM
{
    public partial struct EmbedMenu<UserT, ChannelT, EMHistMemory, EMActsMemory>
        where UserT: IUser 
        where ChannelT: ITextChannel 
        where EMHistMemory : struct, IInsideMemory<EmbedMenu<UserT, ChannelT, EMHistMemory, EMActsMemory>>
        where EMActsMemory : struct, IInsideMemory<EmbedMenu<UserT, ChannelT, EMHistMemory, EMActsMemory>.EmbedMenuAct>
    {
        public delegate void EmbedMenuDel(ref EmbedMenu<UserT, ChannelT, EMHistMemory, EMActsMemory> EM);
        
        //TODO: Test for possible regression in performance due to inlining
        
        private const MethodImplOptions Opt = MethodImplOptions.AggressiveInlining;
        
        public struct EmbedMenuAct
        {
            public string Emoji, Name, Desc;

            public EmbedMenuDel Act;

            [MethodImpl(Opt)]
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

        internal InsideList<EmbedMenu<UserT, ChannelT, EMHistMemory, EMActsMemory>, EMHistMemory> EMHistory;
        
        internal InsideList<EmbedMenuAct, EMActsMemory> Acts;

        internal UserT User;

        internal ChannelT Channel;

        [MethodImpl(Opt)]
        public EmbedMenu(ref EmbedMenuAct ExecutedEMAct, ref EmbedMenu<UserT, ChannelT, EMHistMemory, EMActsMemory> PrevEM, string title, string desc)
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
            
            //Unsafe.SkipInit() would null the value anyway ( For its a ref type )...and as such isn't helpful

            CurrentMsg = null;
        }
        
        [MethodImpl(Opt)]
        internal EmbedMenu(EmbedMenuDel initAct, UserT user, ChannelT channel, ref InsideList<EmbedMenu<UserT, ChannelT, EMHistMemory, EMActsMemory>, EMHistMemory> emHistory, ref InsideList<EmbedMenuAct, EMActsMemory> acts)
        {
            InitAct = initAct;

            User = user;

            Channel = channel;
            
            EMHistory = emHistory;

            Acts = acts;
            
            CurrentEMIndex = 0;
            
            //Page defaults
            
            CurrentPageNumber = 0;

            MaxElemsPerPage = 5;

            Pages = EMHelpers.DivideAndRoundUpFast(Acts.Count, MaxElemsPerPage);
            
            //Unsafe.SkipInit() would null the values anyway ( For its a ref type )...and as such isn't helpful

            Title = null;

            Desc = null;

            CurrentMsg = null;
        }
    }

    public partial struct EmbedMenu<UserT, ChannelT, EMHistMemory, EMActsMemory>
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
    
        internal async Task Compile(DiscordSocketClient Client)
        {
            CurrentMsg = await Channel.SendMessageAsync(null, false, EMB.Build());
            
            //Setup reaction handler

            Client.ReactionAdded += OnReactionAdded;
        }

        [MethodImpl(Opt)]
        private Task OnReactionAdded(Cacheable<IUserMessage, ulong> Cacheable, ISocketMessageChannel SocketMessageChannel, SocketReaction React)
        {
            var ReactName = React.Emote.Name;
            
            //Starting index would be the PageNumber * MaxElemsPerPage
            
            //E.x. Assuming that MaxElemsPerPage is 5, page number of 0 would mean we start from Index 0 * 5 = 0...
            //Page number of 1 would mean we start from 1 * 5 = 5
            //Such is true since Array Indexes are zero-based
            
            foreach (var Act in Acts)
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
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }

        [MethodImpl(Opt)]
        private void Back()
        {
            if (CurrentEMIndex == 0)
            {
                return;
            }

            ref var PrevEM = ref EMHistory.GetByRef(unchecked(CurrentEMIndex - 1));

            PrevEM.Acts.Clear();
            
            PrevEM.InitAct(ref PrevEM);
        }
    }
}
