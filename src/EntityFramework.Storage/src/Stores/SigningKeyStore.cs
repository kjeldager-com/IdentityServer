// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.EntityFramework.Stores
{
    /// <summary>
    /// Implementation of ISigningKeyStore thats uses EF.
    /// </summary>
    /// <seealso cref="ISigningKeyStore" />
    public class SigningKeyStore : ISigningKeyStore
    {
        /// <summary>
        /// The DbContext.
        /// </summary>
        protected readonly PersistedGrantDbContext Context;

        /// <summary>
        /// The logger.
        /// </summary>
        protected readonly ILogger<SigningKeyStore> Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SigningKeyStore"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="ArgumentNullException">context</exception>
        public SigningKeyStore(PersistedGrantDbContext context, ILogger<SigningKeyStore> logger)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Logger = logger;
        }

        /// <summary>
        /// Loads all keys from store.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<SerializedKey>> LoadKeysAsync()
        {
            var entities = await Context.Keys.ToArrayAsync();
            return entities.Select(key => new SerializedKey
            {
                Id = key.Id,
                Version = key.Version,
                SigningAlgorithm = key.Algorithm,
                Data = key.Data,
                DataProtected = key.DataProtected,
                IsX509Certificate = key.IsX509Certificate
            });
        }

        /// <summary>
        /// Persists new key in store.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task StoreKeyAsync(SerializedKey key)
        {
            var entity = new Key
            {
                Id = key.Id,
                Version = key.Version,
                Algorithm = key.SigningAlgorithm,
                Data = key.Data,
                DataProtected = key.DataProtected,
                IsX509Certificate = key.IsX509Certificate
            };
            Context.Keys.Add(entity);
            return Context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes key from storage.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task DeleteKeyAsync(string id)
        {
            var item = await Context.Keys.FirstOrDefaultAsync(x => x.Id == id);
            if (item != null)
            {
                try
                {
                    Context.Keys.Remove(item);
                    await Context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    Context.Entry(item).State = EntityState.Detached;
                    // already deleted, so we can eat this exception
                    Logger.LogDebug("Concurrency exception caught deleting key id {kid}", id);
                }
            }
        }
    }
}