// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

export interface ISubscriptionDetail {
  baseUrl: string;
  createdTime: string;
  offerName?: any;
  planName?: any;
  primaryKey: string;
  primaryKeySecretName: string;
  secondaryKey: string;
  secondaryKeySecretName: string;
  status: string;
  subscriptionId: string;
  subscriptionName: string;
  owner: string;
}