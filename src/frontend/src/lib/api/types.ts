import type { components, operations, paths } from './schema';

export type ExchangeRatesQuote = components['schemas']['ExchangeRatesQuoteDto'];
export type WatchItem = components['schemas']['WatchItemDto'];
export type PagedWatchlist = components['schemas']['PagedResultOfWatchItemDto'];
export type CreateWatchItemRequest = components['schemas']['CreateWatchItemRequest'];
export type ProblemDetails = components['schemas']['ProblemDetails'];

export type { paths, operations, components };

