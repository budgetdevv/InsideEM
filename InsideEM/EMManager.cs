using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Discord;
using Discord.WebSocket;

namespace InsideEM
{
    public static class EMManager
    {
        public static void GenEmbed<UserT, ChannelT>(EmbedMenu<UserT, ChannelT>.EmbedMenuDel EMDel)
            where UserT : IUser
            where ChannelT : ITextChannel
        {
            ref var EMRef = ref Unsafe.NullRef<EmbedMenu<UserT, ChannelT>>();
            
            EMDel.Invoke(ref EMRef);

            if (!Unsafe.IsNullRef(ref EMRef))
            {
                ssss
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