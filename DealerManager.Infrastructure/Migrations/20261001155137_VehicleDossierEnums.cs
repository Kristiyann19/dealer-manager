using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace DealerManager.Infrastructure.Migrations;
public partial class VehicleDossierEnums : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Preserve unrecognized historical text before converting nullable fields to enum values.
        migrationBuilder.Sql("""
UPDATE "Vehicles" SET "Notes" = concat_ws(E'\n', nullif("Notes", ''), '[Legacy FuelType] ' || "FuelType")
WHERE "FuelType" IS NOT NULL AND btrim("FuelType") <> '' AND regexp_replace(lower(btrim("FuelType")), '[[:space:]_-]', '', 'g') NOT IN ('petrol', 'бензин', 'diesel', 'дизел', 'lpg', 'газ(lpg)', 'cng', 'метан(cng)', 'hybrid', 'хибрид', 'pluginhybrid', 'pluginхибрид', 'electric', 'електрически', 'hydrogen', 'водород', 'other', 'друго', 'gasoline', 'бензин/газ', 'бензингаз', 'газ', 'метан', 'електричество');
ALTER TABLE "Vehicles" ALTER COLUMN "FuelType" TYPE integer USING
(CASE WHEN "FuelType" IS NULL OR btrim("FuelType") = '' THEN NULL ELSE CASE regexp_replace(lower(btrim("FuelType")), '[[:space:]_-]', '', 'g') WHEN 'petrol' THEN 0 WHEN 'бензин' THEN 0 WHEN 'diesel' THEN 1 WHEN 'дизел' THEN 1 WHEN 'lpg' THEN 2 WHEN 'газ(lpg)' THEN 2 WHEN 'cng' THEN 3 WHEN 'метан(cng)' THEN 3 WHEN 'hybrid' THEN 4 WHEN 'хибрид' THEN 4 WHEN 'pluginhybrid' THEN 5 WHEN 'pluginхибрид' THEN 5 WHEN 'electric' THEN 6 WHEN 'електрически' THEN 6 WHEN 'hydrogen' THEN 7 WHEN 'водород' THEN 7 WHEN 'other' THEN 99 WHEN 'друго' THEN 99 WHEN 'gasoline' THEN 0 WHEN 'бензин/газ' THEN 2 WHEN 'бензингаз' THEN 2 WHEN 'газ' THEN 2 WHEN 'метан' THEN 3 WHEN 'електричество' THEN 6 ELSE 99 END END);
UPDATE "Vehicles" SET "Notes" = concat_ws(E'\n', nullif("Notes", ''), '[Legacy Transmission] ' || "Transmission")
WHERE "Transmission" IS NOT NULL AND btrim("Transmission") <> '' AND regexp_replace(lower(btrim("Transmission")), '[[:space:]_-]', '', 'g') NOT IN ('manual', 'ръчна', 'automatic', 'автоматична', 'semiautomatic', 'полуавтоматична', 'cvt', 'безстепенна(cvt)', 'other', 'друго', 'автоматик', 'ръчни', 'автоматични', 'роботизирана');
ALTER TABLE "Vehicles" ALTER COLUMN "Transmission" TYPE integer USING
(CASE WHEN "Transmission" IS NULL OR btrim("Transmission") = '' THEN NULL ELSE CASE regexp_replace(lower(btrim("Transmission")), '[[:space:]_-]', '', 'g') WHEN 'manual' THEN 0 WHEN 'ръчна' THEN 0 WHEN 'automatic' THEN 1 WHEN 'автоматична' THEN 1 WHEN 'semiautomatic' THEN 2 WHEN 'полуавтоматична' THEN 2 WHEN 'cvt' THEN 3 WHEN 'безстепенна(cvt)' THEN 3 WHEN 'other' THEN 99 WHEN 'друго' THEN 99 WHEN 'автоматик' THEN 1 WHEN 'ръчни' THEN 0 WHEN 'автоматични' THEN 1 WHEN 'роботизирана' THEN 2 ELSE 99 END END);
UPDATE "Vehicles" SET "Notes" = concat_ws(E'\n', nullif("Notes", ''), '[Legacy DriveType] ' || "DriveType")
WHERE "DriveType" IS NOT NULL AND btrim("DriveType") <> '' AND regexp_replace(lower(btrim("DriveType")), '[[:space:]_-]', '', 'g') NOT IN ('frontwheeldrive', 'предно', 'rearwheeldrive', 'задно', 'allwheeldrive', 'allwheeldrive(awd)', 'навсичкиколела(awd)', 'fourwheeldrive', 'fourwheeldrive(4wd)', '4×4', 'other', 'друго', 'fwd', 'rwd', 'awd', '4wd', '4x4');
ALTER TABLE "Vehicles" ALTER COLUMN "DriveType" TYPE integer USING
(CASE WHEN "DriveType" IS NULL OR btrim("DriveType") = '' THEN NULL ELSE CASE regexp_replace(lower(btrim("DriveType")), '[[:space:]_-]', '', 'g') WHEN 'frontwheeldrive' THEN 0 WHEN 'предно' THEN 0 WHEN 'rearwheeldrive' THEN 1 WHEN 'задно' THEN 1 WHEN 'allwheeldrive' THEN 2 WHEN 'allwheeldrive(awd)' THEN 2 WHEN 'навсичкиколела(awd)' THEN 2 WHEN 'fourwheeldrive' THEN 3 WHEN 'fourwheeldrive(4wd)' THEN 3 WHEN '4×4' THEN 3 WHEN 'other' THEN 99 WHEN 'друго' THEN 99 WHEN 'fwd' THEN 0 WHEN 'rwd' THEN 1 WHEN 'awd' THEN 2 WHEN '4wd' THEN 3 WHEN '4x4' THEN 3 ELSE 99 END END);
UPDATE "Vehicles" SET "Notes" = concat_ws(E'\n', nullif("Notes", ''), '[Legacy BodyType] ' || "BodyType")
WHERE "BodyType" IS NOT NULL AND btrim("BodyType") <> '' AND regexp_replace(lower(btrim("BodyType")), '[[:space:]_-]', '', 'g') NOT IN ('sedan', 'седан', 'hatchback', 'хечбек', 'estate', 'комби', 'suv', 'suv/джип', 'coupe', 'купе', 'convertible', 'кабриолет', 'minivan', 'миниван', 'van', 'ван', 'pickup', 'пикап', 'other', 'друго', 'saloon', 'wagon', 'stationwagon', 'джип', 'coupé');
ALTER TABLE "Vehicles" ALTER COLUMN "BodyType" TYPE integer USING
(CASE WHEN "BodyType" IS NULL OR btrim("BodyType") = '' THEN NULL ELSE CASE regexp_replace(lower(btrim("BodyType")), '[[:space:]_-]', '', 'g') WHEN 'sedan' THEN 0 WHEN 'седан' THEN 0 WHEN 'hatchback' THEN 1 WHEN 'хечбек' THEN 1 WHEN 'estate' THEN 2 WHEN 'комби' THEN 2 WHEN 'suv' THEN 3 WHEN 'suv/джип' THEN 3 WHEN 'coupe' THEN 4 WHEN 'купе' THEN 4 WHEN 'convertible' THEN 5 WHEN 'кабриолет' THEN 5 WHEN 'minivan' THEN 6 WHEN 'миниван' THEN 6 WHEN 'van' THEN 7 WHEN 'ван' THEN 7 WHEN 'pickup' THEN 8 WHEN 'пикап' THEN 8 WHEN 'other' THEN 99 WHEN 'друго' THEN 99 WHEN 'saloon' THEN 0 WHEN 'wagon' THEN 2 WHEN 'stationwagon' THEN 2 WHEN 'джип' THEN 3 WHEN 'coupé' THEN 4 ELSE 99 END END);
UPDATE "Vehicles" SET "Notes" = concat_ws(E'\n', nullif("Notes", ''), '[Legacy EuroStandard] ' || "EuroStandard")
WHERE "EuroStandard" IS NOT NULL AND btrim("EuroStandard") <> '' AND regexp_replace(lower(btrim("EuroStandard")), '[[:space:]_-]', '', 'g') NOT IN ('euro1', 'евро1', 'euro2', 'евро2', 'euro3', 'евро3', 'euro4', 'евро4', 'euro5', 'евро5', 'euro6', 'евро6', 'euro6dtemp', 'евро6dtemp', 'euro6d', 'евро6d', 'euro7', 'евро7', 'other', 'друго');
ALTER TABLE "Vehicles" ALTER COLUMN "EuroStandard" TYPE integer USING
(CASE WHEN "EuroStandard" IS NULL OR btrim("EuroStandard") = '' THEN NULL ELSE CASE regexp_replace(lower(btrim("EuroStandard")), '[[:space:]_-]', '', 'g') WHEN 'euro1' THEN 0 WHEN 'евро1' THEN 0 WHEN 'euro2' THEN 1 WHEN 'евро2' THEN 1 WHEN 'euro3' THEN 2 WHEN 'евро3' THEN 2 WHEN 'euro4' THEN 3 WHEN 'евро4' THEN 3 WHEN 'euro5' THEN 4 WHEN 'евро5' THEN 4 WHEN 'euro6' THEN 5 WHEN 'евро6' THEN 5 WHEN 'euro6dtemp' THEN 6 WHEN 'евро6dtemp' THEN 6 WHEN 'euro6d' THEN 7 WHEN 'евро6d' THEN 7 WHEN 'euro7' THEN 8 WHEN 'евро7' THEN 8 WHEN 'other' THEN 99 WHEN 'друго' THEN 99 ELSE 99 END END);
""");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
ALTER TABLE "Vehicles" ALTER COLUMN "FuelType" TYPE text USING
(CASE "FuelType" WHEN 0 THEN 'Petrol' WHEN 1 THEN 'Diesel' WHEN 2 THEN 'Lpg' WHEN 3 THEN 'Cng' WHEN 4 THEN 'Hybrid' WHEN 5 THEN 'PlugInHybrid' WHEN 6 THEN 'Electric' WHEN 7 THEN 'Hydrogen' WHEN 99 THEN 'Other' ELSE NULL END);
ALTER TABLE "Vehicles" ALTER COLUMN "Transmission" TYPE text USING
(CASE "Transmission" WHEN 0 THEN 'Manual' WHEN 1 THEN 'Automatic' WHEN 2 THEN 'SemiAutomatic' WHEN 3 THEN 'Cvt' WHEN 99 THEN 'Other' ELSE NULL END);
ALTER TABLE "Vehicles" ALTER COLUMN "DriveType" TYPE text USING
(CASE "DriveType" WHEN 0 THEN 'FrontWheelDrive' WHEN 1 THEN 'RearWheelDrive' WHEN 2 THEN 'AllWheelDrive' WHEN 3 THEN 'FourWheelDrive' WHEN 99 THEN 'Other' ELSE NULL END);
ALTER TABLE "Vehicles" ALTER COLUMN "BodyType" TYPE text USING
(CASE "BodyType" WHEN 0 THEN 'Sedan' WHEN 1 THEN 'Hatchback' WHEN 2 THEN 'Estate' WHEN 3 THEN 'Suv' WHEN 4 THEN 'Coupe' WHEN 5 THEN 'Convertible' WHEN 6 THEN 'Minivan' WHEN 7 THEN 'Van' WHEN 8 THEN 'Pickup' WHEN 99 THEN 'Other' ELSE NULL END);
ALTER TABLE "Vehicles" ALTER COLUMN "EuroStandard" TYPE text USING
(CASE "EuroStandard" WHEN 0 THEN 'Euro1' WHEN 1 THEN 'Euro2' WHEN 2 THEN 'Euro3' WHEN 3 THEN 'Euro4' WHEN 4 THEN 'Euro5' WHEN 5 THEN 'Euro6' WHEN 6 THEN 'Euro6dTemp' WHEN 7 THEN 'Euro6d' WHEN 8 THEN 'Euro7' WHEN 99 THEN 'Other' ELSE NULL END);
""");
    }
}
