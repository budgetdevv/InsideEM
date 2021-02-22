using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Discord;
using Discord.WebSocket;
using InsideEM.Collections;
using InsideEM.Memory;

namespace InsideEM.EmbedMenu
{
    public static class EMManager
    {
        internal static readonly DiscordSocketClient Client;

        private static readonly ConcurrentDictionary<ulong, byte> ActiveUsers;

        [ThreadStatic]
        private static EmbedBuilder EMB;

        //TODO: Test for possible regression in performance due to inlining
        
        private const MethodImplOptions Opt = MethodImplOptions.AggressiveInlining;
        
        static EMManager()
        {
            ActiveUsers = new ConcurrentDictionary<ulong, byte>();
        }

        [MethodImpl(Opt)]
        public static EmbedBuilder GetBuilder()
        {
            if (EMB == null)
            {
                return EMB = new EmbedBuilder();
            }

            return EMB;
        }
        
        [MethodImpl(Opt)]
        public static void GenEmbed<UserT, ChannelT>(UserT User, ChannelT Channel, EmbedMenu<UserT, ChannelT>.EmbedMenuDel EMDel)
            where UserT : IUser
            where ChannelT : ITextChannel
        {
            //TODO: Find out if Unsafe.SkipInit() would cause performance regression
            
            Unsafe.SkipInit(out byte Trash);
            
            if (!ActiveUsers.TryAdd(User.Id, Trash))
            {
                ssss
                
                return;
            }
            
            GenEmbedUnchecked(User, Channel, EMDel);
        }
        
        [MethodImpl(Opt)]
        public static void GenEmbedUnchecked<UserT, ChannelT>(UserT User, ChannelT Channel, EmbedMenu<UserT, ChannelT>.EmbedMenuDel EMDel)
            where UserT : IUser
            where ChannelT : ITextChannel
        {
            //Allocate arrays!

            var EMHistory = new InsideList<EmbedMenu<UserT, ChannelT>>(5);

            var Acts = new InsideList<EmbedMenu<UserT, ChannelT>.EmbedMenuAct>(5);

            var EM = new EmbedMenu<UserT, ChannelT>(EMDel, User, Channel, ref EMHistory, ref Acts);
                
            ref var EMRef = ref EM;
            
            EMDel.Invoke(ref EMRef);

            if (Unsafe.IsNullRef(ref EMRef))
            {
                ActiveUsers.Remove(User.Id, out _);
                
                return;
            }

            _ = EMRef.Compile(Client);
        }

        [MethodImpl(Opt)]
        public static void EMCancel<ChannelT, EMHistMemory, EMActsMemory>(ref EMRaw<ChannelT, EMHistMemory, EMActsMemory> EM)
            where ChannelT : ITextChannel
            where EMHistMemory : struct, IInsideMemory<EMRaw<ChannelT, EMHistMemory, EMActsMemory>>
            where EMActsMemory : struct, IInsideMemory<EMRaw<ChannelT, EMHistMemory, EMActsMemory>.EmbedMenuAct>
        {
            EM.Decompile();

            _ = EM.CurrentMsg.DeleteAsync();

            ActiveUsers.Remove(EM.User.Id, out _);
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