using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Discord;
using Discord.WebSocket;

namespace InsideEM
{
    public static class EMManager
    {
        private static readonly DiscordSocketClient Client;
        
        public static void GenEmbed<UserT, ChannelT>(EmbedMenu<UserT, ChannelT>.EmbedMenuDel EMDel)
            where UserT : IUser
            where ChannelT : ITextChannel
        {
            ref var EMRef = ref Unsafe.NullRef<EmbedMenu<UserT, ChannelT>>();
            
            EMDel.Invoke(ref EMRef);

            if (!Unsafe.IsNullRef(ref EMRef))
            {
                _ = EMRef.Compile(Client);
            }
        }

        public static void LetsGo()
        {
            GenEmbed((ref EmbedMenu<SocketUser, SocketTextChannel> EM) =>
            {
                EM = new EmbedMenu<SocketUser, SocketTextChannel>()
            });
        }
    }
}