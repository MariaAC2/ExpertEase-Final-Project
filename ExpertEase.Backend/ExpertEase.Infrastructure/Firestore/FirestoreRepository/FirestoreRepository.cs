using ExpertEase.Domain.Entities;
using Google.Cloud.Firestore;

namespace ExpertEase.Infrastructure.Firestore.FirestoreRepository;

public class FirestoreRepository(FirestoreDb firestoreDb) : IFirestoreRepository
{
    public async Task<T?> GetAsync<T>(string collection, string id, CancellationToken cancellationToken = default)
        where T : FirestoreBaseEntityDto
    {
        var doc = await firestoreDb.Collection(collection).Document(id.ToString()).GetSnapshotAsync(cancellationToken);
        return doc.Exists ? doc.ConvertTo<T>() : null;
    }
    
    public async Task<List<T>> ListAsync<T>(
        string collection,
        CancellationToken cancellationToken = default
    ) where T : FirestoreBaseEntityDto
    {
        var collectionRef = firestoreDb.Collection(collection);
        var snapshot = await collectionRef.GetSnapshotAsync(cancellationToken);

        return snapshot.Documents.Select(doc => doc.ConvertTo<T>()).ToList();
    }
    
    public async Task<T?> GetAsync<T>(string collection, Func<CollectionReference, Query> queryBuilder, CancellationToken cancellationToken = default)
        where T : FirestoreBaseEntityDto
    {
        var collectionRef = firestoreDb.Collection(collection);
        var query = queryBuilder(collectionRef);

        var snapshot = await query.GetSnapshotAsync(cancellationToken);

        var doc = snapshot.Documents.FirstOrDefault();
        if (doc == null)
            return null;

        var entity = doc.ConvertTo<T>();
        entity.Id = doc.Id;
        return entity;
    }
    
    public async Task<List<T>> ListAsync<T>(string collection, Func<CollectionReference, Query> queryBuilder, CancellationToken cancellationToken = default) where T : FirestoreBaseEntityDto
    {
        var collectionRef = firestoreDb.Collection(collection);
        var query = queryBuilder(collectionRef);

        var snapshot = await query.GetSnapshotAsync(cancellationToken);
        return snapshot.Documents
            .Select(doc => doc.ConvertTo<T>())
            .ToList();
    }
    
    public async Task<List<TDto>> ListAsync<T, TDto>(string collection, Func<T, TDto> mapper, CancellationToken cancellationToken = default)
        where T : FirestoreBaseEntityDto
    {
        var entities = await ListAsync<T>(collection, cancellationToken);
        return entities.Select(mapper).ToList();
    }
    
    public async Task<T> AddAsync<T>(string collection, T entity, CancellationToken cancellationToken = default)
        where T : FirestoreBaseEntityDto
    {
        entity.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

        await firestoreDb.Collection(collection)
            .Document(entity.Id.ToString())
            .SetAsync(entity, cancellationToken: cancellationToken);

        return entity;
    }

    public async Task<T> UpdateAsync<T>(string collection, T entity, CancellationToken cancellationToken = default)
        where T : FirestoreBaseEntityDto
    {
        await firestoreDb.Collection(collection)
            .Document(entity.Id.ToString())
            .SetAsync(entity, SetOptions.Overwrite, cancellationToken);

        return entity;
    }

    public async Task DeleteAsync<T>(string collection, string id, CancellationToken cancellationToken = default)
        where T : FirestoreBaseEntityDto
    {
        await firestoreDb.Collection(collection)
            .Document(id)
            .DeleteAsync(cancellationToken: cancellationToken);
    }
}