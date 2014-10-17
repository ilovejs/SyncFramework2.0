--
-- Create databases for the Sync Framework peer synchronization samples
-- that use SqlSyncProvider.
--
USE master
GO

IF EXISTS (SELECT [name] FROM [master].[sys].[databases] 
			   WHERE [name] = N'SyncSamplesDb_SqlPeer1')
	BEGIN
	
		DECLARE @SQL varchar(max)
		SELECT @SQL = '';
	
		SELECT @SQL = COALESCE(@SQL,'') + 'Kill ' + Convert(varchar, SPId) + ';' 
		FROM master..sysprocesses 
		WHERE DBId = DB_ID('SyncSamplesDb_SqlPeer1') AND SPId <> @@SPId AND loginame <> 'sa'

		EXEC(@SQL)
		
		DROP DATABASE SyncSamplesDb_SqlPeer1
		
	END

CREATE DATABASE SyncSamplesDb_SqlPeer1
GO

USE SyncSamplesDb_SqlPeer1
GO

CREATE SCHEMA Sales
GO

CREATE SCHEMA Sync
GO


IF EXISTS (SELECT [name] FROM [master].[sys].[databases] 
			   WHERE [name] = N'SyncSamplesDb_SqlPeer2')
	
	BEGIN
	
		DECLARE @SQL varchar(max)
		SELECT @SQL = '';
	
		SELECT @SQL = COALESCE(@SQL,'') + 'Kill ' + Convert(varchar, SPId) + ';' 
		FROM master..sysprocesses 
		WHERE DBId = DB_ID('SyncSamplesDb_SqlPeer2') AND SPId <> @@SPId AND loginame <> 'sa'

		EXEC(@SQL)
		
		DROP DATABASE SyncSamplesDb_SqlPeer2
		
	END

CREATE DATABASE SyncSamplesDb_SqlPeer2
GO

USE SyncSamplesDb_SqlPeer2
GO

CREATE SCHEMA Sales
GO

CREATE SCHEMA Sync
GO


USE SyncSamplesDb_SqlPeer1
GO

------------------------------------
--
-- Create two tables for the Sync Framework synchronization samples.
--

CREATE TABLE Sales.Customer(
	CustomerId uniqueidentifier NOT NULL PRIMARY KEY DEFAULT NEWID(), 
	CustomerName nvarchar(100) NOT NULL,
	SalesPerson nvarchar(100) NOT NULL,
	CustomerType nvarchar(100) NOT NULL)

CREATE TABLE Sales.CustomerContact(
	CustomerId uniqueidentifier NOT NULL,
	PhoneNumber nvarchar(100) NOT NULL,
	PhoneType nvarchar(100) NOT NULL,
	CONSTRAINT PK_CustomerContact PRIMARY KEY (CustomerId, PhoneType))

ALTER TABLE Sales.CustomerContact
ADD CONSTRAINT FK_CustomerContact_Customer FOREIGN KEY (CustomerId)
	REFERENCES Sales.Customer (CustomerId)
	
GO

-- Create a procedure to perform a backup and restore
-- of the database. Create it in SyncSamplesDb_SqlPeer2
-- so that we can call restore for SyncSamplesDb_SqlPeer1.

USE SyncSamplesDb_SqlPeer2
GO

CREATE PROCEDURE usp_SampleDbBackupRestore(
	@Operation varchar(10)
)

AS

SET @Operation = UPPER(@Operation)

IF @Operation = 'BACKUP'
BEGIN

	BACKUP DATABASE [SyncSamplesDb_SqlPeer1] 
	TO  DISK = N'C:\Program Files\Microsoft SQL Server\MSSQL10.MSSQLSERVER\MSSQL\Backup\SyncSamplesDb_SqlPeer1.bak' 
	WITH NOFORMAT, NOINIT,  NAME = N'SyncSamplesDb_SqlPeer1-Full Database Backup', SKIP, NOREWIND, NOUNLOAD,  STATS = 10
	
END

ELSE IF @Operation = 'RESTORE'
BEGIN

	-- Kill any connections to the database, so that the RESTORE operation can execute.
	DECLARE @SQL varchar(max)
	SELECT @SQL = '';

	SELECT @SQL = COALESCE(@SQL,'') + 'Kill ' + Convert(varchar, SPId) + ';' 
	FROM master..sysprocesses 
	WHERE DBId = DB_ID('SyncSamplesDb_SqlPeer1') AND SPId <> @@SPId AND loginame <> 'sa'

	EXEC(@SQL)
	-- Backup the tail of the log.
	BACKUP LOG [SyncSamplesDb_SqlPeer1] 
	TO  DISK = N'C:\Program Files\Microsoft SQL Server\MSSQL10.MSSQLSERVER\MSSQL\Backup\SyncSamplesDb_SqlPeer1.bak' 
	WITH  NO_TRUNCATE , NOFORMAT, NOINIT,  NAME = N'TestBackupRestore-Transaction Log  Backup', SKIP, NOREWIND, NOUNLOAD,  NORECOVERY ,  STATS = 10

	-- Restore the database.
	RESTORE DATABASE [SyncSamplesDb_SqlPeer1] 
	FROM  DISK = N'C:\Program Files\Microsoft SQL Server\MSSQL10.MSSQLSERVER\MSSQL\Backup\SyncSamplesDb_SqlPeer1.bak' 
	WITH FILE = 1,  NOUNLOAD,  STATS = 10

END

ELSE
	PRINT('Unrecognized operation. @Operation must be set to BACKUP or RESTORE.')

GO

USE SyncSamplesDb_SqlPeer1

GO

------------------------------------
-- Insert test data.
--
--
-- Wrap the inserts in a procedure so that each snippet
-- can call the procedure to reset the database after
-- the snippet completes.
CREATE PROCEDURE usp_ResetSqlPeerData

AS
	SET NOCOUNT ON
	
	DELETE FROM Sales.CustomerContact
	DELETE FROM Sales.Customer
	
	--INSERT INTO Customer.
	INSERT INTO Sales.Customer (CustomerName, SalesPerson, CustomerType) VALUES (N'Aerobic Exercise Company', N'James Bailey', N'Wholesale')
	INSERT INTO Sales.Customer (CustomerName, SalesPerson, CustomerType) VALUES (N'Exemplary Cycles', N'James Bailey', N'Retail')
	INSERT INTO Sales.Customer (CustomerName, SalesPerson, CustomerType) VALUES (N'Tandem Bicycle Store', N'Brenda Diaz', N'Wholesale')
	INSERT INTO Sales.Customer (CustomerName, SalesPerson, CustomerType) VALUES (N'Rural Cycle Emporium', N'Brenda Diaz', N'Retail')
	INSERT INTO Sales.Customer (CustomerName, SalesPerson, CustomerType) VALUES (N'Sharp Bikes', N'Brenda Diaz', N'Retail')

	--Declare variables that are used in subsequent inserts.
	DECLARE @CustomerId uniqueidentifier
	DECLARE @InsertString nvarchar(1024)

	--INSERT INTO CustomerContact.
	SELECT @CustomerId = CustomerId FROM Sales.Customer WHERE CustomerName = N'Exemplary Cycles'
	SET @InsertString = 'INSERT INTO Sales.CustomerContact (CustomerId, PhoneNumber, PhoneType) VALUES (''' + CAST(@CustomerId AS nvarchar(38)) + ''', ''959-555-0151'', ''Business'')'
	EXECUTE sp_executesql @InsertString

	SELECT @CustomerId = CustomerId FROM Sales.Customer WHERE CustomerName = N'Tandem Bicycle Store'
	SET @InsertString = 'INSERT INTO Sales.CustomerContact (CustomerId, PhoneNumber, PhoneType) VALUES (''' + CAST(@CustomerId AS nvarchar(38)) + ''', ''107-555-0138'', ''Business'')'
	EXECUTE sp_executesql @InsertString

	SELECT @CustomerId = CustomerId FROM Sales.Customer WHERE CustomerName = N'Rural Cycle Emporium'
	SET @InsertString = 'INSERT INTO Sales.CustomerContact (CustomerId, PhoneNumber, PhoneType) VALUES (''' + CAST(@CustomerId AS nvarchar(38)) + ''', ''158-555-0142'', ''Business'')'
	EXECUTE sp_executesql @InsertString

	SELECT @CustomerId = CustomerId FROM Sales.Customer WHERE CustomerName = N'Rural Cycle Emporium'
	SET @InsertString = 'INSERT INTO Sales.CustomerContact (CustomerId, PhoneNumber, PhoneType) VALUES (''' + CAST(@CustomerId AS nvarchar(38)) + ''', ''453-555-0167'', ''Mobile'')'
	EXECUTE sp_executesql @InsertString

	SET NOCOUNT OFF

GO

EXEC usp_ResetSqlPeerData

GO

-- The following code is used to drop and recreate objects so that
-- sample applications can be run multiple times. This code is not
-- required by Sync Framework.

CREATE PROCEDURE usp_CleanupAfterAppRun
AS
	SET NOCOUNT ON
	
	EXEC usp_ResetSqlPeerData
	
	DECLARE @ObjName nvarchar(100);
	DECLARE @SchemaName nvarchar(100);

	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_delete_trigger'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP TRIGGER ' + @SchemaName + '.' + @ObjName)
	
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_insert_trigger'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP TRIGGER ' + @SchemaName + '.' + @ObjName)
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_update_trigger'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP TRIGGER ' + @SchemaName + '.' + @ObjName)	
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_delete_trigger'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP TRIGGER ' + @SchemaName + '.' + @ObjName)
	
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_insert_trigger'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP TRIGGER ' + @SchemaName + '.' + @ObjName)
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_update_trigger'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP TRIGGER ' + @SchemaName + '.' + @ObjName)	
	
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_tracking'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP TABLE ' + @SchemaName + '.' + @ObjName)
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_tracking'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP TABLE ' + @SchemaName + '.' + @ObjName)

	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'scope_config'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP TABLE ' + @SchemaName + '.' + @ObjName)


	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'scope_info'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP TABLE ' + @SchemaName + '.' + @ObjName)
		

	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_delete'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)
			
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_deletemetadata'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)			
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_insert'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)	
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_insertmetadata'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_selectchanges'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_selectrow'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_update'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_updatemetadata'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)	
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_delete'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)
			
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_deletemetadata'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)			
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_insert'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)	
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_insertmetadata'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_selectchanges'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_selectrow'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_update'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_updatemetadata'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)																	
		
GO



USE SyncSamplesDb_SqlPeer2

GO

CREATE PROCEDURE usp_CleanupAfterAppRun
AS
	SET NOCOUNT ON
	
	DECLARE @ObjName nvarchar(100);
	DECLARE @SchemaName nvarchar(100);	
	
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_tracking'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP TABLE ' + @SchemaName + '.' + @ObjName)
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_tracking'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP TABLE ' + @SchemaName + '.' + @ObjName)

	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'scope_config'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP TABLE ' + @SchemaName + '.' + @ObjName)


	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'scope_info'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP TABLE ' + @SchemaName + '.' + @ObjName)
		

	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_delete'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)
			
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_deletemetadata'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)			
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_insert'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)	
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_insertmetadata'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_selectchanges'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_selectrow'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_update'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer_updatemetadata'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)	
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_delete'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)
			
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_deletemetadata'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)			
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_insert'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)	
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_insertmetadata'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_selectchanges'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_selectrow'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_update'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)
		
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact_updatemetadata'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP PROCEDURE ' + @SchemaName + '.' + @ObjName)	
			
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'Customer'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP TABLE ' + @SchemaName + '.' + @ObjName)	
			
	SELECT @ObjName = o.[name], @SchemaName = s.[name] FROM [sys].[objects] o
		JOIN [sys].[schemas] s
		ON o.[schema_id] = s.[schema_id]
		WHERE o.[name] = N'CustomerContact'
		
		IF LEN(@ObjName) > 0 
			EXEC('DROP TABLE ' + @SchemaName + '.' + @ObjName)																	
		
GO

