IF NOT EXISTS (
    SELECT * FROM sys.objects WHERE type = 'P' AND name = 'AddData'
)
BEGIN
    EXEC('
    CREATE PROCEDURE AddData
        @TableName NVARCHAR(128),
        @Columns NVARCHAR(MAX), -- JSON array: [{Key, SqlType}]
        @Rows NVARCHAR(MAX)     -- JSON array: [{Key1: val, Key2: val}]
    AS
    BEGIN
        SET NOCOUNT ON;

        DECLARE @createTableSql NVARCHAR(MAX) = '''';
        DECLARE @insertSql NVARCHAR(MAX) = '''';
        DECLARE @colList NVARCHAR(MAX) = '''';
        DECLARE @jsonSchema NVARCHAR(MAX) = '''';

        -- Create table if not exists
        IF NOT EXISTS (
            SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName
        )
        BEGIN
            SELECT @createTableSql = ''CREATE TABLE '' + QUOTENAME(@TableName) + '' ('' +
                STRING_AGG(
                    QUOTENAME(JSON_VALUE(j.value, ''''$.Key'''')) + '' '' + JSON_VALUE(j.value, ''''$.SqlType''''),
                    '',''
                ) +
                '')''
            FROM OPENJSON(@Columns) AS j;

            EXEC(@createTableSql);
        END

        -- Build column list and JSON schema for OPENJSON WITH clause
        SELECT 
            @colList = STRING_AGG(QUOTENAME(JSON_VALUE(j.value, ''''$.Key'''')), '',''),
            @jsonSchema = STRING_AGG(
                JSON_VALUE(j.value, ''''$.Key'''') + '' '' + JSON_VALUE(j.value, ''''$.SqlType''''),
                '', ''
            )
        FROM OPENJSON(@Columns) AS j;

        -- Insert data using OPENJSON with parameter
        SET @insertSql = ''
        INSERT INTO '' + QUOTENAME(@TableName) + '' ('' + @colList + '')
        SELECT '' + @colList + ''
        FROM OPENJSON(@RowsParam)
        WITH ('' + @jsonSchema + '')'';

        EXEC sp_executesql @insertSql, N''@RowsParam NVARCHAR(MAX)'', @RowsParam = @Rows;
    END
    ')
END
