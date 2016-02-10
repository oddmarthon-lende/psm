CREATE VIEW [dbo].[metadata]
AS
(
	SELECT m.Id, n.Namespace, k.Name AS [Key], m.Value FROM [meta] m left join [namespaces] n on (n.Id = m.NamespaceId) left join keys k on (k.Id = m.KeyId)
)
