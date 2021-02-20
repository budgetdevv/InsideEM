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

        private EmbedMenu<UserT, ChannelT>[] EMArr;

        private string Title, Desc;

        private PooledList<EmbedMenuAct> Acts;

        private int CurrentIndex, CurrentPageNumber, Pages, MaxElemsPerPage;

        private UserT User;

        private ChannelT Channel;

        [MethodImpl(EMHelpers.InlineAndOptimize)]
        public EmbedMenu(EmbedMenuDel initAct, EmbedMenu<UserT, ChannelT>[] emArr, string title, string desc, PooledList<EmbedMenuAct> acts, UserT user, ChannelT channel)
        {
            InitAct = initAct;
            
            EMArr = emArr;

            Title = title;

            Desc = desc;

            Acts = acts;

            CurrentIndex = 0;
            
            //Page defaults
            
            CurrentPageNumber = 0;

            MaxElemsPerPage = 5;

            Pages = EMHelpers.DivideAndRoundUpFast(Acts.Count, MaxElemsPerPage);

            //User creds
            
            User = user;

            Channel = channel;
            
            Unsafe.SkipInit(out CurrentMsg);
        }
        
        [MethodImpl(EMHelpers.InlineAndOptimize)]
        public EmbedMenu(ref EmbedMenuAct ExecutedEMAct, ref EmbedMenu<UserT, ChannelT> PrevEM, string title, string desc)
        {
            InitAct = ExecutedEMAct.Act;
            
            EMArr = PrevEM.EMArr;

            Title = title;

            Desc = desc;

            Acts = PrevEM.Acts;

            CurrentIndex = unchecked(++PrevEM.CurrentIndex);
            
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
            
            if (ReactName == CrossEmoji)
            {
                return Task.CompletedTask;
            }

            if (ReactName == BackEmoji)
            {
                Back();
                
                return Task.CompletedTask;
            }
            
            return Task.CompletedTask;
        }

        [MethodImpl(EMHelpers.InlineAndOptimize)]
        private void Back()
        {
            if (CurrentIndex == 0)
            {
                return;
            }

            ref var PrevEM = ref EMArr[unchecked(CurrentIndex - 1)];

            PrevEM.Acts.Clear();
            
            PrevEM.InitAct(ref PrevEM);
        }
    }
}