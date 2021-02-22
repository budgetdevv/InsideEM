using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using InsideEM.Memory;

namespace InsideEM.EmbedMenu
{
    public static class EMHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DivideAndRoundUpFast(int Num, int Divisor)
        {
            unchecked
            {
                var Remainder = Num % Divisor;

                if (Remainder == 0)
                {
                    return Num / Divisor;
                }

                return ((Num - Remainder) / Divisor) + 1;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetStartingIndex<UserT, ChannelT, EMHistMemory, EMActsMemory>(ref EMRaw<UserT, ChannelT, EMHistMemory, EMActsMemory> EM) 
            where UserT : IUser 
            where ChannelT : ITextChannel
            where EMHistMemory : struct, IInsideMemory<EMRaw<UserT, ChannelT, EMHistMemory, EMActsMemory>>
            where EMActsMemory : struct, IInsideMemory<EMRaw<UserT, ChannelT, EMHistMemory, EMActsMemory>.EmbedMenuAct>
        {
            //Starting index would be the PageNumber * MaxElemsPerPage
            
            //E.x. Assuming that MaxElemsPerPage is 5, page number of 0 would mean we start from Index 0 * 5 = 0...
            //Page number of 1 would mean we start from 1 * 5 = 5
            //Such is true since Array Indexes are zero-based

            return unchecked(EM.CurrentPageNumber * EM.MaxElemsPerPage);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPageReactCount<UserT, ChannelT, EMHistMemory, EMActsMemory>(int StartingIndex, ref EMRaw<UserT, ChannelT, EMHistMemory, EMActsMemory> EM) 
            where UserT : IUser 
            where ChannelT : ITextChannel
            where EMHistMemory : struct, IInsideMemory<EMRaw<UserT, ChannelT, EMHistMemory, EMActsMemory>>
            where EMActsMemory : struct, IInsideMemory<EMRaw<UserT, ChannelT, EMHistMemory, EMActsMemory>.EmbedMenuAct>
        {
            //If count = 2, and we start from Index 1 ( Which points to the second elem ), then the react count would be 2 - 1 = 1
            
            var Diff = unchecked(EM.Acts.Count - StartingIndex);

            return Diff > EM.MaxElemsPerPage ? EM.Pages : Diff;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanEditMsg<UserT, ChannelT, EMHistMemory, EMActsMemory>(DiscordSocketClient Client, ref EMRaw<UserT, ChannelT, EMHistMemory, EMActsMemory> EM)
            where UserT : IUser 
            where ChannelT : ITextChannel
            where EMHistMemory : struct, IInsideMemory<EMRaw<UserT, ChannelT, EMHistMemory, EMActsMemory>>
            where EMActsMemory : struct, IInsideMemory<EMRaw<UserT, ChannelT, EMHistMemory, EMActsMemory>.EmbedMenuAct>
        {
            if (!EMRaw<UserT, ChannelT, EMHistMemory, EMActsMemory>.IsGuildChannel)
            {
                return false;
            }

            var SocketTextChannel = Unsafe.As<ChannelT, SocketTextChannel>(ref EM.Channel);

            return SocketTextChannel.Guild.GetUser(Client.CurrentUser.Id).GuildPermissions.ManageMessages;
        }
        
        //TODO: Find out if stuff before await keyword gets inlined
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task SendMessageWithPrev<UserT, ChannelT, EMHistMemory, EMActsMemory>(EMRaw<UserT, ChannelT, EMHistMemory, EMActsMemory> Prev, EMRaw<UserT, ChannelT, EMHistMemory, EMActsMemory> Current, EmbedBuilder EMB)
            where UserT : IUser 
            where ChannelT : ITextChannel
            where EMHistMemory : struct, IInsideMemory<EMRaw<UserT, ChannelT, EMHistMemory, EMActsMemory>>
            where EMActsMemory : struct, IInsideMemory<EMRaw<UserT, ChannelT, EMHistMemory, EMActsMemory>.EmbedMenuAct>
        {
            Prev.Decompile();
            
            if (!EMRaw<UserT, ChannelT, EMHistMemory, EMActsMemory>.IsGuildChannel)
            {
                goto NoEdit;
            }

            var SocketTextChannel = Unsafe.As<ChannelT, SocketTextChannel>(ref Current.Channel);

            if (!SocketTextChannel.Guild.GetUser(EMManager.Client.CurrentUser.Id).GuildPermissions.ManageMessages)
            {
                goto NoEdit;
            }

            Current.CurrentMsg = Prev.CurrentMsg;

            await Current.CurrentMsg.ModifyAsync(x => x.Embed = EMB.Build());

            return;
            
            NoEdit:
            {
                _ = Prev.CurrentMsg.DeleteAsync();

                Current.CurrentMsg = await Current.Channel.SendMessageAsync(null, false, EMB.Build());
            }
        }
    }
    
}