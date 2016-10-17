CREATE PROCEDURE [dbo].[usp_clean]
	@Before datetime
AS

	declare @name varchar(max), @q varchar(max);

	declare c cursor for
	select name from sys.tables where name like 'tbl_%';

	open c;

	fetch next from c into @name;

	while(@@FETCH_STATUS = 0)
	begin
		
		set @q = 'delete from [' + @name + '] where [Timestamp] < @Before;'

		exec sp_executesql @q, N'@Before datetime', @Before = @Before;

		fetch next from c into @name;

	end
	
RETURN 0
