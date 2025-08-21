using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LightJson;
using Plugins.UnisaveSteamMicrotransactions.Backend.Steam;
using Unisave.Arango;
using Unisave.Contracts;
using Unisave.Facades;
using Unisave.Serialization;
using Unisave.Serialization.Context;
using Unisave.SteamMicrotransactions.Steam.Steam;

namespace Unisave.SteamMicrotransactions.Steam
{
    /// <summary>
    /// Job that calls the GetReport Steam API for the transactions in the
    /// past 90 days (configurable) and appends this Steam-side info to
    /// transaction entities in the database.
    ///
    /// The GetReport API is documented here:
    /// https://partner.steamgames.com/doc/webapi/ISteamMicroTxn#GetReport
    /// </summary>
    public class ProcessSteamReportJob
    {
        /*
           To test this job, you can erase all the data it writes into
           the database with this query and then run it:
           
           FOR entity IN steam_microtransactions
               UPDATE {
                   _key: entity._key,
                   SteamReportOrder: null,
                   SteamReportOrderTimestamp: null
               } IN steam_microtransactions
         */
        
        private readonly SteamMicrotransactionsConfig config;
        private readonly SteamWebMtxApi steamApi;
        private readonly IArango arango;

        public ProcessSteamReportJob(
            SteamMicrotransactionsConfig config,
            SteamWebMtxApi steamApi,
            IArango arango
        )
        {
            this.config = config;
            this.steamApi = steamApi;
            this.arango = arango;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            // this value sweeps across time from past to present
            DateTime processedUntil = DateTime.UtcNow.Subtract(
                TimeSpan.FromDays(config.ReconcileTransactionsYoungerThanDays)
            );

            while (true)
            {
                // call GetReport API from that time and retrieve results
                List<ReportOrder> orders = await steamApi.GetReport(
                    type: "GAMESALES",
                    time: processedUntil,
                    maxResults: 1_000,
                    cancellationToken: cancellationToken
                );
                
                // check cancellation
                cancellationToken.ThrowIfCancellationRequested();
                
                // write the fetched data into the database
                JsonValue jsonOrders = Serializer.ToJson(
                    orders,
                    SerializationContext.ServerToServerStorage
                );
                JsonValue jsonNow = Serializer.ToJson(
                    DateTime.UtcNow,
                    SerializationContext.ServerToServerStorage
                );
                var entities = new RawAqlQuery(arango, @"
                    FOR order IN @orders
                        FOR entity IN steam_microtransactions
                            FILTER entity.OrderId == order.orderid
                            UPDATE {
                                _key: entity._key,
                                SteamReportOrder: order,
                                SteamReportOrderTimestamp: @now
                            } IN steam_microtransactions
                            RETURN NEW
                ")
                    .Bind("orders", jsonOrders)
                    .Bind("now", jsonNow)
                    .GetAs<SteamTransactionEntity>();
                
                // check cancellation
                cancellationToken.ThrowIfCancellationRequested();
                
                // check if items are to be revoked and do that
                foreach (SteamTransactionEntity entity in entities)
                {
                    await CheckItemRevocationsForTransaction(entity);
                    cancellationToken.ThrowIfCancellationRequested();
                }
                
                // if there is only one or no result, break the loop
                // (one item will result in us fetching it again next iteration)
                if (orders.Count <= 1)
                    break;

                // advance the processedUntil to the time of the last result
                processedUntil = orders[orders.Count - 1].Time;
            }
        }

        /// <summary>
        /// Checks if there are items to be revoked from the player,
        /// because a transaction was refunded or found to be fraudulent
        /// </summary>
        private Task CheckItemRevocationsForTransaction(
            SteamTransactionEntity transaction
        )
        {
            // I'm leaving this up to a future implementation...
            
            /*
             * First check the status value returned by the GetReport API
             * https://partner.steamgames.com/doc/features/microtransactions/implementation#status_values
             *
             * If it's one of (Refunded, PartialRefund, Chargedback,
             * RefundedSuspectedFraud, RefundedFriendlyFraud) then you need pair
             * up items and see which of them are in this state. Then you also
             * check which items have already been revoked (which needs to be
             * stored inside them). If items align and an item is to be revoked,
             * you call the logic to do the revoking and then save the entity.
             *
             * To revoke the item, the Product class needs a RevokeFromPlayer
             * method, analogous to the GiveToPlayer method. The problem is
             * that we're now running outside the request scope. So either we
             * create a scoped IoC container with a registered auth provider
             * for the user stored in transaction.UnisavePlayerId, which would
             * then be resolvable via Auth.Get(), OR let the user do the
             * resolution themselves based on transaction.UnisavePlayerId,
             * but we put into conflict the Product's constructor dep injection
             * either way. This needs further pondering.
             *
             * Don't forget that if you add fields to the entity, they must
             * be validated or cleared during transaction proposal in the
             * PurchasingServerFacet.
             *
             * ALSO! Running code outside the request scope is not really
             * tested within Unisave. For example the DB. and Log. facades
             * seem not to work. We need a framework update first.
             * (the guard for facades does not check IoC container but
             * request context which is wrong for global services like the DB)
             */

            return Task.CompletedTask;
        }
    }
}