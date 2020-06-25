using System;
using System.Threading;
using System.Threading.Tasks;
using HBaseNet.HRpc;
using HBaseNet.Region;
using HBaseNet.Utility;
using HBaseNet.Zk;
using Microsoft.Extensions.Logging;
using Pb;

namespace HBaseNet
{
    public class AdminClient : CommonClient, IAdminClient
    {
        private RegionClient _adminClient;

        public AdminClient(string zkQuorum)
        {
            ZkQuorum = zkQuorum;
        }

        public async Task<IAdminClient> Build(CancellationToken? token = null)
        {
            token ??= DefaultCancellationSource.Token;
            var res = await LocateMasterClient(token.Value);
            return res ? this : null;
        }

        private async Task<bool> LocateMasterClient(CancellationToken token)
        {
            if (_adminClient != null) return true;
            var master = await TryLocateResource(ZkHelper.HBaseMaster, Master.Parser.ParseFrom,
                token);

            _adminClient = await new RegionClient(master.Master_.HostName, (ushort) master.Master_.Port,
                    RegionType.MasterService)
                .Build(RetryCount, token);
            if (_adminClient != null)
                _logger.LogInformation($"Locate master server at : {_adminClient.Host}:{_adminClient.Port}");

            return _adminClient != null;
        }

        private async Task<bool> checkProcedureWithBackoff(ulong procId, CancellationToken token)
        {
            var backoff = BackoffStart;
            var oldTime = DateTime.Now;
            while (DateTime.Now - oldTime < Timeout)
            {
                var req = new GetProcedureStateCall(procId);
                await _adminClient.QueueRPC(req);
                var res = await _adminClient.GetRPCResult(req.CallId);
                if (res?.Msg is GetProcedureResultResponse rep)
                    switch (rep.State)
                    {
                        case GetProcedureResultResponse.Types.State.NotFound:
                            return false;
                        case GetProcedureResultResponse.Types.State.Finished:
                            return true;
                    }

                backoff = await TaskEx.SleepAndIncreaseBackoff(backoff, BackoffIncrease, token);
            }

            return false;
        }

        public async Task<bool> CreateTable(CreateTableCall t, CancellationToken? token = null)
        {
            token ??= DefaultCancellationSource.Token;
            await _adminClient.QueueRPC(t);
            var res = await _adminClient.GetRPCResult(t.CallId);
            if (res?.Msg is CreateTableResponse create)
            {
                return await checkProcedureWithBackoff(create.ProcId, token.Value);
            }

            return false;
        }

        public async Task<bool> DeleteTable(DeleteTableCall t, CancellationToken? token = null)
        {
            token ??= DefaultCancellationSource.Token;
            await _adminClient.QueueRPC(t);
            var res = await _adminClient.GetRPCResult(t.CallId);
            if (res?.Msg is DeleteTableResponse del)
            {
                return await checkProcedureWithBackoff(del.ProcId, token.Value);
            }

            return false;
        }

        public async Task<bool> EnableTable(EnableTableCall t, CancellationToken? token = null)
        {
            token ??= DefaultCancellationSource.Token;
            await _adminClient.QueueRPC(t);
            var res = await _adminClient.GetRPCResult(t.CallId);
            if (res?.Msg is EnableTableResponse enb)
            {
                return await checkProcedureWithBackoff(enb.ProcId, token.Value);
            }

            return false;
        }

        public async Task<bool> DisableTable(DisableTableCall t, CancellationToken? token = null)
        {
            token ??= DefaultCancellationSource.Token;
            await _adminClient.QueueRPC(t);
            var res = await _adminClient.GetRPCResult(t.CallId);
            if (res?.Msg is DisableTableResponse dis)
            {
                return await checkProcedureWithBackoff(dis.ProcId, token.Value);
            }

            return false;
        }

        public void Dispose()
        {
            _adminClient?.Dispose();
            DefaultCancellationSource.Cancel();
        }
    }
}