using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Discord;
using Discord.WebSocket;

namespace InsideEM
{
    public static class EMManager
    {
        private static readonly DiscordSocketClient Client;

        private static readonly ConcurrentDictionary<ulong, byte> ActiveUsers;

        static EMManager()
        {
            ActiveUsers = new ConcurrentDictionary<ulong, byte>();
        }
        
        [MethodImpl(EMHelpers.InlineAndOptimize)]
        public static void GenEmbed<UserT, ChannelT>(UserT User, ChannelT Channel, EmbedMenu<UserT, ChannelT>.EmbedMenuDel EMDel)
            where UserT : IUser
            where ChannelT : ITextChannel
        {
            Unsafe.SkipInit(out byte Trash);
            
            if (!ActiveUsers.TryAdd(User.Id, Trash))
            {
                ssss
                
                return;
            }
            
            GenEmbedUnchecked(User, Channel, EMDel);
        }
        
        [MethodImpl(EMHelpers.InlineAndOptimize)]
        public static void GenEmbedUnchecked<UserT, ChannelT>(UserT User, ChannelT Channel, EmbedMenu<UserT, ChannelT>.EmbedMenuDel EMDel)
            where UserT : IUser
            where ChannelT : ITextChannel
        {
            Unsafe.SkipInit(out EmbedMenu<UserT, ChannelT> EM);

            //We have to populate context-related data for the first time
            
            EM.User = User;

            EM.Channel = Channel;
            
            ref var EMRef = ref EM;
            
            EMDel.Invoke(ref EMRef);

            if (Unsafe.IsNullRef(ref EMRef))
            {
                ActiveUsers.Remove(User.Id, out _);
                
                return;
            }

            _ = EMRef.Compile(Client);
        }

        // public static void LetsGo()
        // {
        //     GenEmbed((ref EmbedMenu<SocketUser, SocketTextChannel> EM) =>
        //     {
        //         EM = new EmbedMenu<SocketUser, SocketTextChannel>();
        //     });
        // }
    }
}