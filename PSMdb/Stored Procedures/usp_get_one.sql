﻿CREATE PROCEDURE [dbo].[usp_get_one]
	@Namespace VARCHAR(MAX),
	@Key VARCHAR(MAX)
AS
	SELECT top 1 [Value], [Timestamp] from data where [Namespace] = @Namespace and [Key] = @Key order by [Timestamp] desc;
RETURN 0