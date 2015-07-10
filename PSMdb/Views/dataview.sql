CREATE VIEW [dbo].[data]
	AS SELECT vt.Id, nt.Namespace, kt.Name as [Key], vt.Value, vt.[Timestamp] FROM [values] vt
		LEFT JOIN [namespaces] nt on (nt.Id = vt.NamespaceId)
		LEFT JOIN [keys] kt on (kt.Id = vt.KeyId);
