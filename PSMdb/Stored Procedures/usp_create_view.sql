CREATE PROCEDURE [dbo].[usp_create_view]
	@basetype nvarchar(100)
AS
	
	declare @q nvarchar(max);

	exec usp_create_table @basetype;

	if(object_id(@basetype + '_data', N'view') is null)
	begin

		set @q = N'CREATE VIEW [dbo].[' + @basetype + '_data]
					AS (
						SELECT vt.Id, nt.Namespace, kt.Name as [Key], vt.Value, vt.[Timestamp] FROM [tbl_' + @basetype + '] vt
						LEFT JOIN [namespaces] nt on (nt.Id = vt.NamespaceId)
						LEFT JOIN [keys] kt on (kt.Id = vt.KeyId)
					);';

		exec sp_executesql @q;

	end

RETURN 0
