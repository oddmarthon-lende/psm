CREATE PROCEDURE [dbo].[usp_clean]
	@Before datetime
AS
	delete from [values] where [Timestamp] < @Before;
RETURN 0
