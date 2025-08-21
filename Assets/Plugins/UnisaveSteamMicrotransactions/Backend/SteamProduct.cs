using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unisave.Foundation;
using Unisave.Utils;

namespace Unisave.SteamMicrotransactions.Steam
{
    /// <summary>
    /// Interface for a virtual product (item)
    /// that can be purchased via a Steam microtransaction
    /// </summary>
    public abstract class SteamProduct
    {
        /// <summary>
        /// Your own unique identifier for the item (product).
        /// Is passed to steam API to give a machine-readable identifier to the
        /// product on the steam-side.
        /// </summary>
        public abstract uint ItemId { get; }

        /// <summary>
        /// Cost of this item in every currency in which
        /// a transaction could be initiated. The cost is
        /// per one virtual item (product) and it is in
        /// currency units, not cents.
        ///
        /// Keys are ISO 4217 currency codes. Supported currencies are:
        /// https://partner.steamgames.com/doc/store/pricing/currencies
        /// </summary>
        public abstract IReadOnlyDictionary<string, decimal> UnitCost { get; }

        /// <summary>
        /// Description of this item in every language in which
        /// a transaction could be initiated. This text will
        /// be displayed to the player by Steam during checkout.
        ///
        /// Keys are ISO 639-1 language codes. Supported languages are:
        /// https://partner.steamgames.com/doc/store/localization/languages
        /// </summary>
        public abstract IReadOnlyDictionary<string, string> Description { get; }

        /// <summary>
        /// Optional category for the item. This value is used
        /// for grouping sales data in backend Steam reporting
        /// and is never displayed to the player.
        /// </summary>
        public abstract string Category { get; }

        /// <summary>
        /// Constructs the localized product info for the given currency
        /// and language. Both the currency and the language must be supported
        /// by the product.
        /// </summary>
        public LocalizedProductInfo GetLocalizedInfo(
            string currency,
            string language
        )
        {
            if (!UnitCost.ContainsKey(currency))
                throw new ArgumentException(
                    $"Currency {currency} is not supported " +
                    $"by product {GetType().FullName}."
                );
            
            if (!Description.ContainsKey(language))
                throw new ArgumentException(
                    $"Language {language} is not supported " +
                    $"by product {GetType().FullName}."
                );

            return new LocalizedProductInfo(
                itemId: ItemId,
                currency: currency,
                language: language,
                unitCost: UnitCost[currency],
                description: Description[language],
                category: Category,
                productTypeClassName: GetType().FullName
            );
        }

        /// <summary>
        /// Called when a transaction succeeds and the purchased product
        /// should be given to the player. The number of times the product was
        /// purchased is provided as an argument.
        /// </summary>
        public virtual void GiveToPlayer(int quantity)
        {
            // override this
        }
        
        /// <summary>
        /// Called when a transaction succeeds and the purchased product
        /// should be given to the player. The number of times the product was
        /// purchased is provided as an argument.
        /// </summary>
        public virtual Task GiveToPlayerAsync(int quantity)
        {
            // for async, override this
            
            GiveToPlayer(quantity);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates an instance of a steam product of the given type full name
        /// </summary>
        public static SteamProduct CreateInstance(
            string typeFullName,
            IContainer services
        )
        {
            Type type = Type.GetType(typeFullName);
            
            if (type == null)
                throw new ArgumentException(
                    $"Cannot find product {typeFullName}"
                );
            
            return CreateInstance(type, services);
        }

        /// <summary>
        /// Creates an instance of a steam product of the given type
        /// </summary>
        public static SteamProduct CreateInstance(
            Type productType,
            IContainer services
        )
        {
            // check proper parent
            if (!typeof(SteamProduct).IsAssignableFrom(productType))
                throw new InstantiationException(
                    $"Provided type {productType} does not inherit from "
                    + $"the {typeof(SteamProduct)} class."
                );

            // let the ioc container create the instance
            return (SteamProduct) services.Resolve(productType);
        }
    }
}