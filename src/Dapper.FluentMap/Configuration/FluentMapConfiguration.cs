using System;
using System.Linq;
using System.ComponentModel;
using Dapper.FluentMap.Conventions;
using Dapper.FluentMap.Mapping;
using Dapper.FluentMap.Naming;

namespace Dapper.FluentMap.Configuration
{
    /// <summary>
    /// Defines methods for configuring Dapper.FluentMap.
    /// </summary>
    public class FluentMapConfiguration
    {
        /// <summary>
        /// Adds the specified <see cref="T:Dapper.FluentMap.Mapping.EntityMap"/> to the configuration of Dapper.FluentMap.
        /// </summary>
        /// <typeparam name="TEntity">The type argument of the entity.</typeparam>
        /// <param name="mapper">
        /// An instance of the <see cref="T:Dapper.FluentMap.Mapping.IEntityMap"/> interface containing the
        /// entity mapping configuration.
        /// </param>
        public void AddMap<TEntity>(IEntityMap<TEntity> mapper) where TEntity : class
        {
            if (mapper == null)
            {
                throw new ArgumentNullException(nameof(mapper));
            }

            FluentMapper.Registry.AddEntityMap(mapper);
        }

        /// <summary>
        /// Adds the specified <see cref="T:Dapper.FluentMap.Conventions.Convention"/> to the configuration of Dapper.FluentMap.
        /// </summary>
        /// <typeparam name="TConvention">The type of the convention.</typeparam>
        /// <returns>
        /// An instance of <see cref="T:Dapper.FluentMap.Configuration.FluentConventionConfiguration"/>
        /// which allows configuration of the convention.
        /// </returns>
        public FluentConventionConfiguration AddConvention<TConvention>() where TConvention : Convention, new()
        {
            return new FluentConventionConfiguration(new TConvention());
        }

        /// <summary>
        /// Adds a naming policy to the configuration of Dapper.FluentMap.
        /// </summary>
        /// <param name="namingPolicy">The naming policy used to transform member names into column names.</param>
        /// <param name="caseSensitive">A value indicating whether the generated column name mappings should be case sensitive.</param>
        /// <returns>
        /// An instance of <see cref="T:Dapper.FluentMap.Configuration.FluentConventionConfiguration"/>
        /// which allows configuration of the naming policy for entities.
        /// </returns>
        public FluentConventionConfiguration UseNamingPolicy(NamingPolicy namingPolicy, bool caseSensitive = true)
        {
            if (namingPolicy == null)
            {
                throw new ArgumentNullException(nameof(namingPolicy));
            }

            return new FluentConventionConfiguration(new NamingPolicyConvention(namingPolicy, caseSensitive));
        }

        /// <summary>
        /// Adds a custom naming policy to the configuration of Dapper.FluentMap.
        /// </summary>
        /// <param name="transformer">A function that receives a member name and returns a column name.</param>
        /// <param name="caseSensitive">A value indicating whether the generated column name mappings should be case sensitive.</param>
        /// <returns>
        /// An instance of <see cref="T:Dapper.FluentMap.Configuration.FluentConventionConfiguration"/>
        /// which allows configuration of the naming policy for entities.
        /// </returns>
        public FluentConventionConfiguration UseNamingPolicy(Func<string, string> transformer, bool caseSensitive = true)
        {
            if (transformer == null)
            {
                throw new ArgumentNullException(nameof(transformer));
            }

            return UseNamingPolicy(NamingPolicy.Custom(transformer), caseSensitive);
        }

        #region EditorBrowsableStates
        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            return base.ToString();
        }

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new Type GetType()
        {
            return base.GetType();
        }
        #endregion
    }
}
