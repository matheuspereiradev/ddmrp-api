using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("Permissions");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasMaxLength(300);
            builder.Property(p => p.Description).HasMaxLength(200);
            builder.Property(p => p.Module).HasMaxLength(100);

            builder.HasData(
                new Permission { Id = "center:POST", Description = "Create center", Module = "Center" },
                new Permission { Id = "center:GET", Description = "List centers", Module = "Center" },
                new Permission { Id = "center:PUT", Description = "Update center", Module = "Center" },
                new Permission { Id = "center:DELETE", Description = "Delete center", Module = "Center" },

                new Permission { Id = "role:POST", Description = "Create role", Module = "Role" },
                new Permission { Id = "role:GET", Description = "List roles", Module = "Role" },
                new Permission { Id = "role:PUT", Description = "Update role", Module = "Role" },
                new Permission { Id = "role:DELETE", Description = "Delete role", Module = "Role" },

                new Permission { Id = "product:POST", Description = "Create product", Module = "Product" },
                new Permission { Id = "product:GET", Description = "List products", Module = "Product" },
                new Permission { Id = "product:PUT", Description = "Update product", Module = "Product" },
                new Permission { Id = "product:DELETE", Description = "Delete product", Module = "Product" },

                new Permission { Id = "partner:POST", Description = "Create partner", Module = "Partner" },
                new Permission { Id = "partner:GET", Description = "List partners", Module = "Partner" },
                new Permission { Id = "partner:PUT", Description = "Update partner", Module = "Partner" },
                new Permission { Id = "partner:DELETE", Description = "Delete partner", Module = "Partner" },

                new Permission { Id = "setting:PUT", Description = "Update working days settings", Module = "Setting" },

                new Permission { Id = "holiday:POST", Description = "Create holiday", Module = "Holiday" },
                new Permission { Id = "holiday:GET", Description = "List holidays", Module = "Holiday" },
                new Permission { Id = "holiday:PUT", Description = "Update holiday", Module = "Holiday" },
                new Permission { Id = "holiday:DELETE", Description = "Delete holiday", Module = "Holiday" },

                new Permission { Id = "tag:POST", Description = "Create tag", Module = "Tag" },
                new Permission { Id = "tag:GET", Description = "List tags", Module = "Tag" },
                new Permission { Id = "tag:PUT", Description = "Update tag", Module = "Tag" },
                new Permission { Id = "tag:DELETE", Description = "Delete tag", Module = "Tag" },

                new Permission { Id = "reason:POST", Description = "Create reason", Module = "Reason" },
                new Permission { Id = "reason:GET", Description = "List reasons", Module = "Reason" },
                new Permission { Id = "reason:PUT", Description = "Update reason", Module = "Reason" },
                new Permission { Id = "reason:DELETE", Description = "Delete reason", Module = "Reason" },

                new Permission { Id = "masterbuffer:POST", Description = "Create master buffer", Module = "MasterBuffer" },
                new Permission { Id = "masterbuffer:GET", Description = "List master buffers", Module = "MasterBuffer" },
                new Permission { Id = "masterbuffer:PUT", Description = "Update master buffer", Module = "MasterBuffer" },
                new Permission { Id = "masterbuffer:DELETE", Description = "Delete master buffer", Module = "MasterBuffer" },

                new Permission { Id = "bufferprofile:POST", Description = "Create buffer profile", Module = "BufferProfile" },
                new Permission { Id = "bufferprofile:GET", Description = "List buffer profiles", Module = "BufferProfile" },
                new Permission { Id = "bufferprofile:PUT", Description = "Update buffer profile", Module = "BufferProfile" },
                new Permission { Id = "bufferprofile:DELETE", Description = "Delete buffer profile", Module = "BufferProfile" },
                new Permission { Id = "bufferprofile/active:PATCH", Description = "Set buffer profile active status", Module = "BufferProfile" },

                new Permission { Id = "allocationgroup:POST", Description = "Create allocation group", Module = "AllocationGroup" },
                new Permission { Id = "allocationgroup:GET", Description = "List allocation groups", Module = "AllocationGroup" },
                new Permission { Id = "allocationgroup/priorizedallocation:GET", Description = "View priorized allocation", Module = "AllocationGroup" },
                new Permission { Id = "allocationgroup/priorizedallocation/run:POST", Description = "Run priorized allocation", Module = "AllocationGroup" },
                new Permission { Id = "allocationgroup:PUT", Description = "Update allocation group", Module = "AllocationGroup" },
                new Permission { Id = "allocationgroup:DELETE", Description = "Delete allocation group", Module = "AllocationGroup" },

                new Permission { Id = "centerproduct:POST", Description = "Create center product", Module = "CenterProduct" },
                new Permission { Id = "centerproduct:GET", Description = "List center products", Module = "CenterProduct" },
                new Permission { Id = "centerproduct:PUT", Description = "Update center product", Module = "CenterProduct" },
                new Permission { Id = "centerproduct:DELETE", Description = "Delete center product", Module = "CenterProduct" },
                new Permission { Id = "centerproduct/allocation-group:PATCH", Description = "Set center product allocation group", Module = "CenterProduct" },
                new Permission { Id = "centerproduct/tag:PATCH", Description = "Set center product tag", Module = "CenterProduct" },
                new Permission { Id = "centerproduct/reason:PATCH", Description = "Set center product reason", Module = "CenterProduct" },

                new Permission { Id = "bufferadjustmentfactor:POST", Description = "Create buffer adjustment factor", Module = "BufferAdjustmentFactor" },
                new Permission { Id = "bufferadjustmentfactor:GET", Description = "List buffer adjustment factors", Module = "BufferAdjustmentFactor" },
                new Permission { Id = "bufferadjustmentfactor:PUT", Description = "Update buffer adjustment factor", Module = "BufferAdjustmentFactor" },
                new Permission { Id = "bufferadjustmentfactor:DELETE", Description = "Delete buffer adjustment factor", Module = "BufferAdjustmentFactor" },
                new Permission { Id = "bufferadjustmentfactor/active:PATCH", Description = "Set buffer adjustment factor active status", Module = "BufferAdjustmentFactor" },

                new Permission { Id = "zoneadjustmentfactor:POST", Description = "Create zone adjustment factor", Module = "ZoneAdjustmentFactor" },
                new Permission { Id = "zoneadjustmentfactor:GET", Description = "List zone adjustment factors", Module = "ZoneAdjustmentFactor" },
                new Permission { Id = "zoneadjustmentfactor:PUT", Description = "Update zone adjustment factor", Module = "ZoneAdjustmentFactor" },
                new Permission { Id = "zoneadjustmentfactor:DELETE", Description = "Delete zone adjustment factor", Module = "ZoneAdjustmentFactor" },
                new Permission { Id = "zoneadjustmentfactor/active:PATCH", Description = "Set zone adjustment factor active status", Module = "ZoneAdjustmentFactor" },

                new Permission { Id = "demandadjustmentfactor:POST", Description = "Create demand adjustment factor", Module = "DemandAdjustmentFactor" },
                new Permission { Id = "demandadjustmentfactor:GET", Description = "List demand adjustment factors", Module = "DemandAdjustmentFactor" },
                new Permission { Id = "demandadjustmentfactor:PUT", Description = "Update demand adjustment factor", Module = "DemandAdjustmentFactor" },
                new Permission { Id = "demandadjustmentfactor:DELETE", Description = "Delete demand adjustment factor", Module = "DemandAdjustmentFactor" },
                new Permission { Id = "demandadjustmentfactor/active:PATCH", Description = "Set demand adjustment factor active status", Module = "DemandAdjustmentFactor" },

                new Permission { Id = "forecast:POST", Description = "Create forecast", Module = "Forecast" },
                new Permission { Id = "forecast:GET", Description = "List forecasts (daily breakdown)", Module = "Forecast" },
                new Permission { Id = "forecast/grouped:GET", Description = "List forecasts (grouped by period)", Module = "Forecast" },
                new Permission { Id = "forecast:PUT", Description = "Update forecast", Module = "Forecast" },
                new Permission { Id = "forecast:DELETE", Description = "Delete forecast", Module = "Forecast" },

                new Permission { Id = "history:POST", Description = "Create history record", Module = "History" },
                new Permission { Id = "history:GET", Description = "List history records", Module = "History" },
                new Permission { Id = "history:PUT", Description = "Update history record", Module = "History" },
                new Permission { Id = "history:DELETE", Description = "Delete history record", Module = "History" },
                new Permission { Id = "history/discard-status:PATCH", Description = "Set history discard status", Module = "History" },

                new Permission { Id = "note:POST", Description = "Create note", Module = "Note" },
                new Permission { Id = "note:GET", Description = "List notes", Module = "Note" },
                new Permission { Id = "note:PUT", Description = "Update note", Module = "Note" },
                new Permission { Id = "note:DELETE", Description = "Delete note", Module = "Note" },

                new Permission { Id = "order:POST", Description = "Create order", Module = "Order" },
                new Permission { Id = "order:GET", Description = "List orders", Module = "Order" },
                new Permission { Id = "order:PUT", Description = "Update order", Module = "Order" },
                new Permission { Id = "order:DELETE", Description = "Delete order", Module = "Order" },

                new Permission { Id = "user:POST", Description = "Create user", Module = "User" },
                new Permission { Id = "user:GET", Description = "List users", Module = "User" },
                new Permission { Id = "user:PUT", Description = "Update user", Module = "User" },
                new Permission { Id = "user:DELETE", Description = "Delete user", Module = "User" },

                new Permission { Id = "ingestion/run:POST", Description = "Run ingestion pipeline", Module = "Ingestion" },

                new Permission { Id = "calculation/run:POST", Description = "Run calculation pipeline", Module = "Calculation" },

                new Permission { Id = "robot/run:POST", Description = "Run robot (ingestion + calculation)", Module = "Robot" },

                new Permission { Id = "report/inventorybuffermanagement:GET", Description = "View inventory buffer management report", Module = "Report" },
                new Permission { Id = "report/inventorybuffermanagement/colorsummary:GET", Description = "View inventory buffer management color summary", Module = "Report" },
                new Permission { Id = "report/openorders/inbounds:GET", Description = "View open orders report", Module = "Report" },
                new Permission { Id = "report/inventoryhistory:GET", Description = "View inventory history report", Module = "Report" },
                new Permission { Id = "report/projectedstockalert:GET", Description = "View projected stock alert report", Module = "Report" },
                new Permission { Id = "report/bufferpenetration:GET", Description = "View buffer penetration report", Module = "Report" },
                new Permission { Id = "report/itemsbybuffercolorhistory:GET", Description = "View items by buffer color history report", Module = "Report" },
                new Permission { Id = "report/accumulatedbufferhistory:GET", Description = "View accumulated buffer history report", Module = "Report" }
            );
        }
    }
}
