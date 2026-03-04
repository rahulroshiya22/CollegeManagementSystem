# ============================================================
# EXPORT ALL 8 DATABASES INTO ONE MERGED SQL SCRIPT
# For upload to Somee.com single database
# ============================================================
# Run: .\export_all_databases.ps1
# Output: merged_database.sql (ready to paste into Somee query runner)
# ============================================================

$serverName = "RAHUL"
$outputFile = "merged_database.sql"

$databases = @(
    @{ Name = "clgmansys_auth"; Tables = @("Users", "Teachers") },
    @{ Name = "clgmansys1"; Tables = @("Departments", "Students") },
    @{ Name = "clgmansys2"; Tables = @("Departments", "Courses") },
    @{ Name = "clgmansys3"; Tables = @("Enrollments") },
    @{ Name = "clgmansys4"; Tables = @("Fees") },
    @{ Name = "clgmansys5"; Tables = @("Attendances") },
    @{ Name = "CMS_AcademicDb"; Tables = @("TimeSlots", "Grades", "Notices", "Messages", "GroupAnnouncements", "Exams", "ExamQuestions", "ExamSubmissions", "ExamAnswers", "ExamResults") },
    @{ Name = "ChatDB"; Tables = @("Conversations", "ChatMessages") }
)

# Mapping: tables that exist in multiple DBs need to be renamed to avoid clashes
# clgmansys1.Departments = stu_Departments (Student service departments)
# clgmansys2.Departments = crs_Departments (Course service departments)
# We'll handle this by taking Departments from student DB and skipping from course DB

$sql = @"
-- ============================================================
-- MERGED DATABASE SCRIPT - NeoVerse CMS
-- Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
-- Source: 8 databases merged into 1 for Somee.com hosting
-- ============================================================

"@

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " NeoVerse CMS - Database Export Tool" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Helper function to generate INSERT statements
function Export-TableData {
    param(
        [string]$Server,
        [string]$Database,
        [string]$Table,
        [string]$TargetTable
    )
    
    Write-Host "  Exporting: [$Database].[$Table] -> [$TargetTable]" -ForegroundColor Yellow
    
    try {
        # Get column names
        $colQuery = "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '$Table' ORDER BY ORDINAL_POSITION"
        $columns = Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query $colQuery -TrustServerCertificate
        
        if ($null -eq $columns -or $columns.Count -eq 0) {
            Write-Host "    WARNING: Table [$Table] not found in [$Database]" -ForegroundColor Red
            return ""
        }
        
        $colNames = ($columns | ForEach-Object { "[$($_.COLUMN_NAME)]" }) -join ", "
        
        # Get data
        $dataQuery = "SELECT * FROM [$Table]"
        $rows = Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query $dataQuery -TrustServerCertificate
        
        $result = ""
        
        if ($null -ne $rows -and @($rows).Count -gt 0) {
            $rowCount = @($rows).Count
            Write-Host "    Found $rowCount rows" -ForegroundColor Green
            
            $result += "-- Data for [$TargetTable] ($rowCount rows)`r`n"
            $result += "SET IDENTITY_INSERT [$TargetTable] ON;`r`n"
            
            foreach ($row in $rows) {
                $values = @()
                foreach ($col in $columns) {
                    $val = $row.($col.COLUMN_NAME)
                    if ($null -eq $val -or $val -is [System.DBNull]) {
                        $values += "NULL"
                    }
                    elseif ($col.DATA_TYPE -in @("int", "bigint", "smallint", "tinyint", "bit", "decimal", "numeric", "float", "real", "money")) {
                        if ($col.DATA_TYPE -eq "bit") {
                            $values += if ($val) { "1" } else { "0" }
                        }
                        else {
                            $values += "$val"
                        }
                    }
                    elseif ($col.DATA_TYPE -in @("datetime", "datetime2", "date", "time", "datetimeoffset")) {
                        $dtVal = [DateTime]$val
                        if ($col.DATA_TYPE -eq "time") {
                            $values += "'$($val.ToString())'"
                        }
                        else {
                            $values += "'$($dtVal.ToString("yyyy-MM-dd HH:mm:ss.fff"))'"
                        }
                    }
                    else {
                        $escaped = $val.ToString().Replace("'", "''")
                        $values += "N'$escaped'"
                    }
                }
                $valStr = $values -join ", "
                $result += "INSERT INTO [$TargetTable] ($colNames) VALUES ($valStr);`r`n"
            }
            
            $result += "SET IDENTITY_INSERT [$TargetTable] OFF;`r`n`r`n"
        }
        else {
            Write-Host "    No data (empty table)" -ForegroundColor DarkGray
            $result += "-- [$TargetTable]: No data`r`n`r`n"
        }
        
        return $result
    }
    catch {
        Write-Host "    ERROR: $($_.Exception.Message)" -ForegroundColor Red
        return "-- ERROR exporting [$Table] from [$Database]: $($_.Exception.Message)`r`n`r`n"
    }
}

# Helper: export CREATE TABLE DDL
function Export-TableSchema {
    param(
        [string]$Server,
        [string]$Database,
        [string]$Table,
        [string]$TargetTable
    )
    
    try {
        $colQuery = @"
SELECT 
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.IS_NULLABLE,
    c.COLUMN_DEFAULT,
    c.NUMERIC_PRECISION,
    c.NUMERIC_SCALE,
    COLUMNPROPERTY(OBJECT_ID('$Table'), c.COLUMN_NAME, 'IsIdentity') AS IsIdentity
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME = '$Table'
ORDER BY c.ORDINAL_POSITION
"@
        $columns = Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query $colQuery -TrustServerCertificate
        
        if ($null -eq $columns -or $columns.Count -eq 0) {
            return "-- Table [$Table] not found in [$Database]`r`n"
        }
        
        $ddl = "CREATE TABLE [$TargetTable] (`r`n"
        $colDefs = @()
        
        foreach ($col in $columns) {
            $def = "    [$($col.COLUMN_NAME)]"
            
            switch ($col.DATA_TYPE) {
                "nvarchar" { 
                    $len = if ($col.CHARACTER_MAXIMUM_LENGTH -eq -1) { "MAX" } else { $col.CHARACTER_MAXIMUM_LENGTH }
                    $def += " NVARCHAR($len)" 
                }
                "varchar" { 
                    $len = if ($col.CHARACTER_MAXIMUM_LENGTH -eq -1) { "MAX" } else { $col.CHARACTER_MAXIMUM_LENGTH }
                    $def += " VARCHAR($len)" 
                }
                "int" { $def += " INT" }
                "bigint" { $def += " BIGINT" }
                "smallint" { $def += " SMALLINT" }
                "tinyint" { $def += " TINYINT" }
                "bit" { $def += " BIT" }
                "decimal" { $def += " DECIMAL($($col.NUMERIC_PRECISION),$($col.NUMERIC_SCALE))" }
                "numeric" { $def += " NUMERIC($($col.NUMERIC_PRECISION),$($col.NUMERIC_SCALE))" }
                "float" { $def += " FLOAT" }
                "real" { $def += " REAL" }
                "money" { $def += " MONEY" }
                "datetime2" { $def += " DATETIME2" }
                "datetime" { $def += " DATETIME" }
                "date" { $def += " DATE" }
                "time" { $def += " TIME" }
                "datetimeoffset" { $def += " DATETIMEOFFSET" }
                "uniqueidentifier" { $def += " UNIQUEIDENTIFIER" }
                "varbinary" { 
                    $len = if ($col.CHARACTER_MAXIMUM_LENGTH -eq -1) { "MAX" } else { $col.CHARACTER_MAXIMUM_LENGTH }
                    $def += " VARBINARY($len)" 
                }
                default { $def += " $($col.DATA_TYPE.ToUpper())" }
            }
            
            if ($col.IsIdentity -eq 1) {
                $def += " IDENTITY(1,1)"
            }
            
            if ($col.IS_NULLABLE -eq "NO") {
                $def += " NOT NULL"
            }
            else {
                $def += " NULL"
            }
            
            $colDefs += $def
        }
        
        $ddl += ($colDefs -join ",`r`n")
        $ddl += "`r`n);`r`n`r`n"
        
        # Get primary key
        $pkQuery = @"
SELECT col.COLUMN_NAME 
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE col ON tc.CONSTRAINT_NAME = col.CONSTRAINT_NAME
WHERE tc.TABLE_NAME = '$Table' AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
"@
        $pkCols = Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query $pkQuery -TrustServerCertificate
        if ($null -ne $pkCols -and @($pkCols).Count -gt 0) {
            $pkNames = ($pkCols | ForEach-Object { "[$($_.COLUMN_NAME)]" }) -join ", "
            $ddl += "ALTER TABLE [$TargetTable] ADD CONSTRAINT [PK_$TargetTable] PRIMARY KEY ($pkNames);`r`n`r`n"
        }
        
        return $ddl
    }
    catch {
        return "-- ERROR getting schema for [$Table]: $($_.Exception.Message)`r`n"
    }
}

# ============================================================
# STEP 1: SCHEMA (CREATE TABLE for all unique tables)
# ============================================================
Write-Host ""
Write-Host "STEP 1: Exporting schemas..." -ForegroundColor Cyan

# We skip Departments from clgmansys2 (Course DB) since we take it from clgmansys1 (Student DB)
# Both have identical Departments tables

$tableMapping = @(
    # Auth DB
    @{ DB = "clgmansys_auth"; SrcTable = "Users"; DstTable = "Users" },
    @{ DB = "clgmansys_auth"; SrcTable = "Teachers"; DstTable = "Teachers" },
    # Student DB
    @{ DB = "clgmansys1"; SrcTable = "Departments"; DstTable = "Departments" },
    @{ DB = "clgmansys1"; SrcTable = "Students"; DstTable = "Students" },
    # Course DB (skip Departments - already from Student DB)
    @{ DB = "clgmansys2"; SrcTable = "Courses"; DstTable = "Courses" },
    # Enrollment DB
    @{ DB = "clgmansys3"; SrcTable = "Enrollments"; DstTable = "Enrollments" },
    # Fee DB
    @{ DB = "clgmansys4"; SrcTable = "Fees"; DstTable = "Fees" },
    # Attendance DB
    @{ DB = "clgmansys5"; SrcTable = "Attendances"; DstTable = "Attendances" },
    # Academic DB
    @{ DB = "CMS_AcademicDb"; SrcTable = "TimeSlots"; DstTable = "TimeSlots" },
    @{ DB = "CMS_AcademicDb"; SrcTable = "Grades"; DstTable = "Grades" },
    @{ DB = "CMS_AcademicDb"; SrcTable = "Notices"; DstTable = "Notices" },
    @{ DB = "CMS_AcademicDb"; SrcTable = "Messages"; DstTable = "Messages" },
    @{ DB = "CMS_AcademicDb"; SrcTable = "GroupAnnouncements"; DstTable = "GroupAnnouncements" },
    @{ DB = "CMS_AcademicDb"; SrcTable = "Exams"; DstTable = "Exams" },
    @{ DB = "CMS_AcademicDb"; SrcTable = "ExamQuestions"; DstTable = "ExamQuestions" },
    @{ DB = "CMS_AcademicDb"; SrcTable = "ExamSubmissions"; DstTable = "ExamSubmissions" },
    @{ DB = "CMS_AcademicDb"; SrcTable = "ExamAnswers"; DstTable = "ExamAnswers" },
    @{ DB = "CMS_AcademicDb"; SrcTable = "ExamResults"; DstTable = "ExamResults" },
    # Chat DB
    @{ DB = "ChatDB"; SrcTable = "Conversations"; DstTable = "Conversations" },
    @{ DB = "ChatDB"; SrcTable = "ChatMessages"; DstTable = "ChatMessages" }
)

# Also export __EFMigrationsHistory from each DB
$migrationDbs = @("clgmansys_auth", "clgmansys1", "clgmansys2", "clgmansys3", "clgmansys4", "clgmansys5", "CMS_AcademicDb", "ChatDB")

# Course DB also has Departments — we take data from BOTH to compare
# But since they should be the same seed data, we only create the table once

$sql += "-- ============================================================`r`n"
$sql += "-- PART 1: DROP EXISTING TABLES (clean slate)`r`n"
$sql += "-- ============================================================`r`n`r`n"

# Drop in reverse dependency order
$dropOrder = @("ChatMessages", "Conversations", "ExamResults", "ExamAnswers", "ExamSubmissions", "ExamQuestions", "Exams", "GroupAnnouncements", "Messages", "Notices", "Grades", "TimeSlots", "Attendances", "Fees", "Enrollments", "Courses", "Students", "Departments", "Teachers", "Users", "__EFMigrationsHistory")
foreach ($t in $dropOrder) {
    $sql += "IF OBJECT_ID('[$t]', 'U') IS NOT NULL DROP TABLE [$t];`r`n"
}
$sql += "`r`n"

$sql += "-- ============================================================`r`n"
$sql += "-- PART 2: CREATE TABLES`r`n"
$sql += "-- ============================================================`r`n`r`n"

foreach ($tm in $tableMapping) {
    Write-Host "  Schema: [$($tm.DB)].[$($tm.SrcTable)] -> [$($tm.DstTable)]" -ForegroundColor Yellow
    $sql += Export-TableSchema -Server $serverName -Database $tm.DB -Table $tm.SrcTable -TargetTable $tm.DstTable
}

# Create __EFMigrationsHistory table
$sql += @"
CREATE TABLE [__EFMigrationsHistory] (
    [MigrationId] NVARCHAR(150) NOT NULL,
    [ProductVersion] NVARCHAR(32) NOT NULL
);
ALTER TABLE [__EFMigrationsHistory] ADD CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId]);

"@

# ============================================================
# STEP 2: DATA (INSERT statements)
# ============================================================
Write-Host ""
Write-Host "STEP 2: Exporting data..." -ForegroundColor Cyan

$sql += "-- ============================================================`r`n"
$sql += "-- PART 3: INSERT DATA`r`n"
$sql += "-- ============================================================`r`n`r`n"

foreach ($tm in $tableMapping) {
    $sql += Export-TableData -Server $serverName -Database $tm.DB -Table $tm.SrcTable -TargetTable $tm.DstTable
}

# Also export Departments data from Course DB as crs_Departments backup
$sql += "-- NOTE: Course DB Departments data (same as Student DB, kept for reference)`r`n"
$sql += "-- If different, merge manually`r`n`r`n"

# Export migration history from all DBs
Write-Host ""
Write-Host "STEP 3: Exporting migration history..." -ForegroundColor Cyan

$sql += "-- ============================================================`r`n"
$sql += "-- PART 4: EF MIGRATION HISTORY`r`n"
$sql += "-- ============================================================`r`n`r`n"

foreach ($mdb in $migrationDbs) {
    try {
        $migQuery = "IF OBJECT_ID('__EFMigrationsHistory', 'U') IS NOT NULL SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory"
        $migs = Invoke-Sqlcmd -ServerInstance $serverName -Database $mdb -Query $migQuery -TrustServerCertificate -ErrorAction SilentlyContinue
        if ($null -ne $migs -and @($migs).Count -gt 0) {
            $sql += "-- Migrations from [$mdb]`r`n"
            foreach ($m in $migs) {
                $sql += "IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'$($m.MigrationId)')`r`n"
                $sql += "INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'$($m.MigrationId)', N'$($m.ProductVersion)');`r`n"
            }
            $sql += "`r`n"
        }
    }
    catch {
        $sql += "-- No migration history in [$mdb]`r`n`r`n"
    }
}

$sql += "-- ============================================================`r`n"
$sql += "-- EXPORT COMPLETE`r`n"
$sql += "-- ============================================================`r`n"

# Write to file
$sql | Out-File -FilePath $outputFile -Encoding UTF8
$fileSize = (Get-Item $outputFile).Length / 1KB

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " EXPORT COMPLETE!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host " Output: $outputFile" -ForegroundColor White
Write-Host " Size: $([math]::Round($fileSize, 1)) KB" -ForegroundColor White
Write-Host ""
Write-Host " NEXT STEPS:" -ForegroundColor Yellow
Write-Host " 1. Create a database on Somee.com" -ForegroundColor White
Write-Host " 2. Open Somee SQL Query runner" -ForegroundColor White
Write-Host " 3. Paste contents of $outputFile" -ForegroundColor White
Write-Host " 4. Execute the script" -ForegroundColor White
Write-Host " 5. Update connection strings in all services" -ForegroundColor White
Write-Host ""

if ($fileSize -gt 30720) {
    Write-Host " WARNING: File is $([math]::Round($fileSize/1024, 1)) MB - may exceed Somee 30MB limit!" -ForegroundColor Red
}
else {
    Write-Host " Size is within Somee 30MB limit" -ForegroundColor Green
}
