using DeltaSyncServer.Entities.DinoPayload;
using DeltaSyncServer.Entities.InventoriesPayload;
using DeltaSyncServer.Services.v2;
using LibDeltaSystem.Entities.CommonNet;
using LibDeltaSystem.RPC.Payloads.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaSyncServer.Tools.RpcSyncEngine
{
    public class RpcSyncEngineDinos : BaseRpcSyncEngine<DinoData>
    {
        public RpcSyncEngineDinos() : base(RPCSyncType.Dino)
        {
        }

        public override object GetRPCEntity(DinoData dino)
        {
            return new NetDino
            {
                tribe_id = dino.tribe_id,
                dino_id = DinosRequestV2.GetDinoId(dino).ToString(),
                is_female = dino.is_female,
                colors = dino.colors,
                colors_hex = new string[6],
                tamed_name = dino.name,
                tamer_name = dino.tamer,
                classname = Program.TrimArkClassname(dino.classname),
                current_stats = DinosRequestV2.ConvertToStatsFloat(dino.current_stats),
                max_stats = DinosRequestV2.ConvertToStatsFloat(dino.max_stats),
                base_levelups_applied = DinosRequestV2.ConvertToStatsInt(dino.points_wild),
                tamed_levelups_applied = DinosRequestV2.ConvertToStatsInt(dino.points_tamed),
                base_level = dino.base_level,
                level = dino.base_level + dino.extra_level,
                experience = dino.experience,
                is_baby = dino.baby,
                baby_age = dino.baby_age,
                next_imprint_time = dino.next_cuddle,
                imprint_quality = dino.imprint_quality,
                location = dino.location,
                status = dino.status,
                taming_effectiveness = 0,
                is_cryo = false,
                experience_points = dino.experience,
                is_alive = true,
                last_sync_time = DateTime.UtcNow
            };
        }

        public override int GetTribeId(DinoData e)
        {
            return e.tribe_id;
        }
    }
}
