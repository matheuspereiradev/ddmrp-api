using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Service.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAllModulePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Description", "Module" },
                values: new object[,]
                {
                    { "ai/ask:POST", "Ask AI assistant", "Ai" },
                    { "ai/chat:GET", "View AI chat history", "Ai" },
                    { "allocationgroup:DELETE", "Delete allocation group", "AllocationGroup" },
                    { "allocationgroup:GET", "List allocation groups", "AllocationGroup" },
                    { "allocationgroup:POST", "Create allocation group", "AllocationGroup" },
                    { "allocationgroup:PUT", "Update allocation group", "AllocationGroup" },
                    { "allocationgroup/priorizedallocation:GET", "View priorized allocation", "AllocationGroup" },
                    { "allocationgroup/priorizedallocation/run:POST", "Run priorized allocation", "AllocationGroup" },
                    { "bufferadjustmentfactor:DELETE", "Delete buffer adjustment factor", "BufferAdjustmentFactor" },
                    { "bufferadjustmentfactor:GET", "List buffer adjustment factors", "BufferAdjustmentFactor" },
                    { "bufferadjustmentfactor:POST", "Create buffer adjustment factor", "BufferAdjustmentFactor" },
                    { "bufferadjustmentfactor:PUT", "Update buffer adjustment factor", "BufferAdjustmentFactor" },
                    { "bufferadjustmentfactor/active:PATCH", "Set buffer adjustment factor active status", "BufferAdjustmentFactor" },
                    { "bufferprofile:DELETE", "Delete buffer profile", "BufferProfile" },
                    { "bufferprofile:GET", "List buffer profiles", "BufferProfile" },
                    { "bufferprofile:POST", "Create buffer profile", "BufferProfile" },
                    { "bufferprofile:PUT", "Update buffer profile", "BufferProfile" },
                    { "bufferprofile/active:PATCH", "Set buffer profile active status", "BufferProfile" },
                    { "calculation/run:POST", "Run calculation pipeline", "Calculation" },
                    { "centerproduct:DELETE", "Delete center product", "CenterProduct" },
                    { "centerproduct:GET", "List center products", "CenterProduct" },
                    { "centerproduct:POST", "Create center product", "CenterProduct" },
                    { "centerproduct:PUT", "Update center product", "CenterProduct" },
                    { "centerproduct/allocation-group:PATCH", "Set center product allocation group", "CenterProduct" },
                    { "centerproduct/reason:PATCH", "Set center product reason", "CenterProduct" },
                    { "centerproduct/tag:PATCH", "Set center product tag", "CenterProduct" },
                    { "demandadjustmentfactor:DELETE", "Delete demand adjustment factor", "DemandAdjustmentFactor" },
                    { "demandadjustmentfactor:GET", "List demand adjustment factors", "DemandAdjustmentFactor" },
                    { "demandadjustmentfactor:POST", "Create demand adjustment factor", "DemandAdjustmentFactor" },
                    { "demandadjustmentfactor:PUT", "Update demand adjustment factor", "DemandAdjustmentFactor" },
                    { "demandadjustmentfactor/active:PATCH", "Set demand adjustment factor active status", "DemandAdjustmentFactor" },
                    { "forecast:DELETE", "Delete forecast", "Forecast" },
                    { "forecast:GET", "List forecasts (daily breakdown)", "Forecast" },
                    { "forecast:POST", "Create forecast", "Forecast" },
                    { "forecast:PUT", "Update forecast", "Forecast" },
                    { "forecast/grouped:GET", "List forecasts (grouped by period)", "Forecast" },
                    { "history:DELETE", "Delete history record", "History" },
                    { "history:GET", "List history records", "History" },
                    { "history:POST", "Create history record", "History" },
                    { "history:PUT", "Update history record", "History" },
                    { "history/discard-status:PATCH", "Set history discard status", "History" },
                    { "ingestion/run:POST", "Run ingestion pipeline", "Ingestion" },
                    { "masterbuffer:DELETE", "Delete master buffer", "MasterBuffer" },
                    { "masterbuffer:GET", "List master buffers", "MasterBuffer" },
                    { "masterbuffer:POST", "Create master buffer", "MasterBuffer" },
                    { "masterbuffer:PUT", "Update master buffer", "MasterBuffer" },
                    { "note:DELETE", "Delete note", "Note" },
                    { "note:GET", "List notes", "Note" },
                    { "note:POST", "Create note", "Note" },
                    { "note:PUT", "Update note", "Note" },
                    { "order:DELETE", "Delete order", "Order" },
                    { "order:GET", "List orders", "Order" },
                    { "order:POST", "Create order", "Order" },
                    { "order:PUT", "Update order", "Order" },
                    { "partner:DELETE", "Delete partner", "Partner" },
                    { "partner:GET", "List partners", "Partner" },
                    { "partner:POST", "Create partner", "Partner" },
                    { "partner:PUT", "Update partner", "Partner" },
                    { "product:DELETE", "Delete product", "Product" },
                    { "product:GET", "List products", "Product" },
                    { "product:POST", "Create product", "Product" },
                    { "product:PUT", "Update product", "Product" },
                    { "reason:DELETE", "Delete reason", "Reason" },
                    { "reason:GET", "List reasons", "Reason" },
                    { "reason:POST", "Create reason", "Reason" },
                    { "reason:PUT", "Update reason", "Reason" },
                    { "report/accumulatedbufferhistory:GET", "View accumulated buffer history report", "Report" },
                    { "report/bufferpenetration:GET", "View buffer penetration report", "Report" },
                    { "report/inventorybuffermanagement:GET", "View inventory buffer management report", "Report" },
                    { "report/inventorybuffermanagement/colorsummary:GET", "View inventory buffer management color summary", "Report" },
                    { "report/inventoryhistory:GET", "View inventory history report", "Report" },
                    { "report/itemsbybuffercolorhistory:GET", "View items by buffer color history report", "Report" },
                    { "report/openorders/inbounds:GET", "View open orders report", "Report" },
                    { "report/projectedstockalert:GET", "View projected stock alert report", "Report" },
                    { "robot/run:POST", "Run robot (ingestion + calculation)", "Robot" },
                    { "role:DELETE", "Delete role", "Role" },
                    { "role:GET", "List roles", "Role" },
                    { "role:POST", "Create role", "Role" },
                    { "role:PUT", "Update role", "Role" },
                    { "tag:DELETE", "Delete tag", "Tag" },
                    { "tag:GET", "List tags", "Tag" },
                    { "tag:POST", "Create tag", "Tag" },
                    { "tag:PUT", "Update tag", "Tag" },
                    { "user:DELETE", "Delete user", "User" },
                    { "user:GET", "List users", "User" },
                    { "user:POST", "Create user", "User" },
                    { "user:PUT", "Update user", "User" },
                    { "workspace:DELETE", "Clear workspace", "Workspace" },
                    { "workspace:PUT", "Update workspace", "Workspace" },
                    { "zoneadjustmentfactor:DELETE", "Delete zone adjustment factor", "ZoneAdjustmentFactor" },
                    { "zoneadjustmentfactor:GET", "List zone adjustment factors", "ZoneAdjustmentFactor" },
                    { "zoneadjustmentfactor:POST", "Create zone adjustment factor", "ZoneAdjustmentFactor" },
                    { "zoneadjustmentfactor:PUT", "Update zone adjustment factor", "ZoneAdjustmentFactor" },
                    { "zoneadjustmentfactor/active:PATCH", "Set zone adjustment factor active status", "ZoneAdjustmentFactor" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "ai/ask:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "ai/chat:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "allocationgroup:DELETE");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "allocationgroup:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "allocationgroup:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "allocationgroup:PUT");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "allocationgroup/priorizedallocation:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "allocationgroup/priorizedallocation/run:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "bufferadjustmentfactor:DELETE");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "bufferadjustmentfactor:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "bufferadjustmentfactor:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "bufferadjustmentfactor:PUT");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "bufferadjustmentfactor/active:PATCH");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "bufferprofile:DELETE");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "bufferprofile:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "bufferprofile:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "bufferprofile:PUT");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "bufferprofile/active:PATCH");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "calculation/run:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "centerproduct:DELETE");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "centerproduct:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "centerproduct:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "centerproduct:PUT");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "centerproduct/allocation-group:PATCH");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "centerproduct/reason:PATCH");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "centerproduct/tag:PATCH");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "demandadjustmentfactor:DELETE");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "demandadjustmentfactor:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "demandadjustmentfactor:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "demandadjustmentfactor:PUT");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "demandadjustmentfactor/active:PATCH");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "forecast:DELETE");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "forecast:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "forecast:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "forecast:PUT");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "forecast/grouped:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "history:DELETE");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "history:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "history:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "history:PUT");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "history/discard-status:PATCH");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "ingestion/run:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "masterbuffer:DELETE");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "masterbuffer:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "masterbuffer:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "masterbuffer:PUT");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "note:DELETE");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "note:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "note:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "note:PUT");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "order:DELETE");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "order:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "order:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "order:PUT");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "partner:DELETE");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "partner:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "partner:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "partner:PUT");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "product:DELETE");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "product:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "product:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "product:PUT");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "reason:DELETE");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "reason:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "reason:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "reason:PUT");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "report/accumulatedbufferhistory:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "report/bufferpenetration:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "report/inventorybuffermanagement:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "report/inventorybuffermanagement/colorsummary:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "report/inventoryhistory:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "report/itemsbybuffercolorhistory:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "report/openorders/inbounds:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "report/projectedstockalert:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "robot/run:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "role:DELETE");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "role:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "role:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "role:PUT");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "tag:DELETE");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "tag:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "tag:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "tag:PUT");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "user:DELETE");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "user:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "user:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "user:PUT");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "workspace:DELETE");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "workspace:PUT");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "zoneadjustmentfactor:DELETE");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "zoneadjustmentfactor:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "zoneadjustmentfactor:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "zoneadjustmentfactor:PUT");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "zoneadjustmentfactor/active:PATCH");
        }
    }
}
