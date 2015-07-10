CREATE PROCEDURE [dbo].[usp_delete]
	@Namespace VARCHAR(MAX),
	@Key VARCHAR(MAX),
	@Start DATETIME = null,
	@End DATETIME = null,
	@Span BIGINT = null
AS
	
	if(@Span is not null)
	begin

		select top 1 @End = [Timestamp] from [values] order by [Timestamp] desc;

		set @End   = COALESCE(@End, GETDATE());
		set @Start = DATEADD(SECOND, 0 - @Span, @End);

	end

	if(@Start is null or @End is null)
	begin
		delete from [values] where [NamespaceId] in (select Id from [namespaces] where [Namespace] = @Namespace) and [KeyId] in (select Id from [keys] where [Name] = @Key)
	end
	else
	begin
		delete from [values] where [NamespaceId] in (select Id from [namespaces] where [Namespace] = @Namespace) and [KeyId] in (select Id from [keys] where [Name] = @Key) and [Timestamp] >= @Start and [Timestamp] <= @End;
	end

	

RETURN 0
