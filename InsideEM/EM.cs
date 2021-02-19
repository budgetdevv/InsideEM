using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Discord;

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
        }
    }

    public partial struct EmbedMenu
    {
        private const string CrossEmoji = "❎";

        private const string BackEmoji = "🔙";

        private const string NavLeftEmoji = "⬅️";
        
        private const string NavRightEmoji = "➡️";

        [MethodImpl(EMHelpers.InlineAndOptimize)]
        internal void Compile()
        {
            
        }
    }
}