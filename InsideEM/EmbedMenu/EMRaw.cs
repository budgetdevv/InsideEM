using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using InsideEM.Collections;
using InsideEM.Memory;

namespace InsideEM.EmbedMenu
{
    public partial struct EMRaw<ChannelT, EMHistMemory, EMActsMemory>
    {
    
        internal static readonly bool IsGuildChannel;
        
        static EMRaw()
        {
            IsGuildChannel = typeof(ChannelT) == typeof(SocketTextChannel);     
        }
    }
    
    public partial struct EMRaw<ChannelT, EMHistMemory, EMActsMemory>
        where ChannelT: ITextChannel 
        where EMHistMemory : struct, IInsideMemory<EMRaw<ChannelT, EMHistMemory, EMActsMemory>>
        where EMActsMemory : struct, IInsideMemory<EMRaw<ChannelT, EMHistMemory, EMActsMemory>.EmbedMenuAct>
    {
        public delegate void EmbedMenuDel(ref EMRaw<ChannelT, EMHistMemory, EMActsMemory> EM);
        
        //TODO: Test for possible regression in performance due to inlining
        
        private const MethodImplOptions Opt = MethodImplOptions.AggressiveInlining;
        public readonly struct EmbedMenuAct
        {
            public readonly string Emoji, Name, Desc;

            public readonly EmbedMenuDel Act;

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

        internal InsideList<EMRaw<ChannelT, EMHistMemory, EMActsMemory>, EMHistMemory> EMHistory;
        
        internal InsideList<EmbedMenuAct, EMActsMemory> Acts;

        internal SocketGuildUser User;

        internal ChannelT Channel;

        [MethodImpl(Opt)]
        public EMRaw(ref EmbedMenuAct ExecutedEMAct, ref EMRaw<ChannelT, EMHistMemory, EMActsMemory> PrevEM, string title, string desc)
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
        internal EMRaw(EmbedMenuDel initAct, SocketGuildUser user, ChannelT channel, ref InsideList<EMRaw<ChannelT, EMHistMemory, EMActsMemory>, EMHistMemory> emHistory, ref InsideList<EmbedMenuAct, EMActsMemory> acts)
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

    public partial struct EMRaw<ChannelT, EMHistMemory, EMActsMemory>
    {
        private const string CrossEmoji = "❎";

        private const string BackEmoji = "🔙";

        private const string NavLeftEmoji = "⬅️";
        
        private const string NavRightEmoji = "➡️";

        internal IUserMessage CurrentMsg;

        [MethodImpl(Opt)]
        public void AddReaction<AllocatorT>(string Emoji, string FieldTitle, string FieldDesc, EmbedMenuDel Act, ref AllocatorT Allocator) 
            where AllocatorT: struct, IInsideMemoryAllocator<EmbedMenuAct, EMActsMemory>
        {
            var NewAct = new EmbedMenuAct(Emoji, FieldTitle, FieldDesc, Act);
            
            Acts.Add(ref NewAct, ref Allocator);
        }
        
        [MethodImpl(Opt)]
        public void RemoveReaction(string Emoji)
        {
            foreach (ref var Act in Acts)
            {
                if (Act.Emoji == Emoji)
                {
                    Acts.Remove(ref Act);
                    
                    return;
                }
            }
        }
        
        [MethodImpl(Opt)]
        public void RemoveAllReactions()
        {
            Acts.Clear();
        }
        
        internal async Task Compile()
        {
            var EMB = EMManager.GetBuilder();
            
            CurrentMsg = await Channel.SendMessageAsync(null, false, EMB.Build());
            
            //Setup reaction handler

            EMManager.Client.ReactionAdded += OnReactionAdded;
        }

        [MethodImpl(Opt)]
        internal void Decompile()
        {
            EMManager.Client.ReactionAdded -= OnReactionAdded;
        }
        
        [MethodImpl(Opt)]
        private Task OnReactionAdded(Cacheable<IUserMessage, ulong> Cacheable, ISocketMessageChannel SocketMessageChannel, SocketReaction React)
        {
            var ReactName = React.Emote.Name;

            if (ReactName == BackEmoji)
            {
                Back();
                
                return Task.CompletedTask;
            }
            
            if (ReactName == CrossEmoji)
            {
                return Task.CompletedTask;
            }

            var StartingIndex = EMHelpers.GetStartingIndex(ref this);
            
            foreach (ref var Act in Acts.GetEnumerator(StartingIndex, EMHelpers.GetPageReactCount(StartingIndex, ref this)))
            {
                if (Act.Emoji == ReactName)
                {
                    continue;
                }
                
                ref var EMRef = ref this;

                Act.Act(ref EMRef);

                // TODO: Fix this - you can't reassign a ref!
                
                // if (Unsafe.IsNullRef(ref EMRef))
                // {
                //     EMManager.EMCancel(ref this);
                //     
                //     return Task.CompletedTask;
                // }
                
                if (Unsafe.AreSame(ref EMRef, ref this))
                {
                    return Task.CompletedTask;
                }
                
                goto Success;
            }
            
            return Task.CompletedTask;
            
            Success:
            {
                return Task.CompletedTask;
            }
        }

        [MethodImpl(Opt)]
        private void Back()
        {
            if (CurrentEMIndex == 0)
            {
                return;
            }

            ref var PrevEM = ref EMHistory.GetByRef(unchecked(CurrentEMIndex - 1));

            //We want to clear up previous acts
            
            PrevEM.Acts.Clear();
            
            PrevEM.InitAct(ref PrevEM);
        }
    }
}
