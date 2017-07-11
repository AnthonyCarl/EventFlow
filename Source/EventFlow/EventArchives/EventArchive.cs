﻿// The MIT License (MIT)
// 
// Copyright (c) 2015-2017 Rasmus Mikkelsen
// Copyright (c) 2015-2017 eBay Software Foundation
// https://github.com/eventflow/EventFlow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.EventArchives.Persistance;
using EventFlow.EventStores;
using EventFlow.Logs;

namespace EventFlow.EventArchives
{
    public class EventArchive : IEventArchive
    {
        private readonly ILog _log;
        private readonly IEventPersistence _eventPersistence;
        private readonly IEventArchivePersistance _eventArchivePersistance;

        public EventArchive(
            ILog log,
            IEventPersistence eventPersistence,
            IEventArchivePersistance eventArchivePersistance)
        {
            _log = log;
            _eventPersistence = eventPersistence;
            _eventArchivePersistance = eventArchivePersistance;
        }

        public async Task<EventArchiveDetails> ArchiveAsync(
            IIdentity identity,
            CancellationToken cancellationToken)
        {
            _log.Debug($"Starting archive of {identity}");

            EventArchiveDetails eventArchiveDetails;
            using (var committedDomainEventStream = await _eventPersistence.OpenReadAsync(
                identity,
                cancellationToken)
                .ConfigureAwait(false))
            {
                eventArchiveDetails = await _eventArchivePersistance.ArchiveAsync(
                    identity,
                    committedDomainEventStream,
                    cancellationToken)
                    .ConfigureAwait(false);
            }

            _log.Debug($"Finished archive of {identity} to {eventArchiveDetails}. DELETING it from event persistence");
            await _eventPersistence.DeleteEventsAsync(
                identity,
                cancellationToken)
                .ConfigureAwait(false);

            return eventArchiveDetails;
        }
    }
}