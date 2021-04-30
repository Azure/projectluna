while getopts "s:r:n:l:q:p:t:c:x:a:u:w:" opt
do
   case "$opt" in
      s ) subscriptionId="$OPTARG" ;;
      r ) resourceGroupName="$OPTARG" ;;
	  l ) region="$OPTARG" ;;
      n ) namePrefix="$OPTARG" ;;
	  q ) sqlUser="$OPTARG" ;;
	  p ) sqlPassword="$OPTARG" ;;
	  t ) aadTenantId="$OPTARG" ;;
	  c ) aadClientId="$OPTARG" ;;
	  x ) aadSecret="$OPTARG" ;;
	  a ) adminUserId="$OPTARG" ;;
	  u ) adminUserName="$OPTARG" ;;
	  w ) createNew="$OPTARG" ;;
      ? ) helpFunction ;; # Print helpFunction in case parameter is non-existent
   esac
done

storageName="${namePrefix}storage"

sqlServerName="${namePrefix}-sqlserver"
sqlDbName="${namePrefix}-sqldb"

keyVaultName="${namePrefix}-keyvault"

appInsightsName="${namePrefix}-appinsights"

functionAppPlanName="${namePrefix}-svrplan"

gatewayFxAppName="${namePrefix}-gateway"
rbacFxAppName="${namePrefix}-rbac"
partnerFxAppName="${namePrefix}-partner"
publishFxAppName="${namePrefix}-publish"
routingFxAppName="${namePrefix}-routing"

az login --only-show-errors

az account set -s $subscriptionId

if [ ${createNew} = "y" ];
then
  az group create --name $resourceGroupName --location $region
  
  # Create an Azure storage account in the resource group.
  az storage account create \
    --name $storageName \
    --location $region \
    --resource-group $resourceGroupName \
    --sku Standard_GRS
    
  # Create SQL server
  az sql server create \
    -l $region \
    -g $resourceGroupName \
    -n $sqlServerName \
    -u $sqlUser \
    -p $sqlPassword
  
  # Create SQL server firewall rule to allow access from Azure services
  az sql server firewall-rule create \
    -g $resourceGroupName \
    -s $sqlServerName \
    -n "azure services" \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0
  
  # Create SQL Database
  az sql db create \
    -g $resourceGroupName \
    -s $sqlServerName \
    -n $sqlDbName \
    --service-objective S0
    
  # Create key vault
  az keyvault create \
    --location $region \
    --name $keyVaultName \
    --resource-group $resourceGroupName
    
  # Create Application insights instance
  az monitor app-insights component create \
    --app $appInsightsName \
    --location $region \
    -g $resourceGroupName \
    --kind web \
    --application-type web
  
  # Create a Premium plan
  az functionapp plan create \
    --name $functionAppPlanName \
    --resource-group $resourceGroupName \
    --location $region \
	--is-linux true \
    --sku EP1
    
  # Create publish service function App
  az functionapp create \
    --name $publishFxAppName \
    --storage-account $storageName \
    --plan $functionAppPlanName \
    --resource-group $resourceGroupName \
    --functions-version 3 \
	--app-insights $appInsightsName \
	--os-type Linux \
	--runtime dotnet
  
  # Create gateway service function App
  az functionapp create \
    --name $gatewayFxAppName \
    --storage-account $storageName \
    --plan $functionAppPlanName \
    --resource-group $resourceGroupName \
    --functions-version 3 \
	--app-insights $appInsightsName \
	--os-type Linux \
	--runtime dotnet
    
  # Create rbac service function App
  az functionapp create \
    --name $rbacFxAppName \
    --storage-account $storageName \
    --plan $functionAppPlanName \
    --resource-group $resourceGroupName \
    --functions-version 3 \
	--app-insights $appInsightsName \
	--os-type Linux \
	--runtime dotnet
    
  # Create partner service function App
  az functionapp create \
    --name $partnerFxAppName \
    --storage-account $storageName \
    --plan $functionAppPlanName \
    --resource-group $resourceGroupName \
    --functions-version 3 \
	--app-insights $appInsightsName \
	--os-type Linux \
	--runtime dotnet
    
  # Create routing service function App
  az functionapp create \
    --name $routingFxAppName \
    --storage-account $storageName \
    --plan $functionAppPlanName \
    --resource-group $resourceGroupName \
    --functions-version 3 \
	--app-insights $appInsightsName \
	--os-type Linux \
	--runtime dotnet

fi

keyVaultScope="/subscriptions/${subscriptionId}/resourcegroups/${resourceGroupName}/providers/Microsoft.KeyVault/vaults/${keyVaultName}"

az functionapp identity assign -g $resourceGroupName -n $publishFxAppName
publishServiceIdentity=$(az functionapp identity show --name $publishFxAppName --resource-group $resourceGroupName | ./jq -r '.principalId')
az keyvault set-policy --name $keyVaultName --secret-permissions get list set delete --object-id $publishServiceIdentity

az functionapp identity assign -g $resourceGroupName -n $partnerFxAppName
partnerServiceIdentity=$(az functionapp identity show --name $partnerFxAppName --resource-group $resourceGroupName | ./jq -r '.principalId')
az keyvault set-policy --name $keyVaultName --secret-permissions get list set delete --object-id $partnerServiceIdentity

az functionapp identity assign -g $resourceGroupName -n $routingFxAppName
routingServiceIdentity=$(az functionapp identity show --name $routingFxAppName --resource-group $resourceGroupName | ./jq -r '.principalId')
az keyvault set-policy --name $keyVaultName --secret-permissions get list set delete --object-id $routingServiceIdentity
  
# Deploy gateway app
az webapp deployment source config-zip \
    -g $resourceGroupName \
	-n $gatewayFxAppName \
    -t 360 \
    --src gatewayfx.zip
	
# Deploy publish app
az webapp deployment source config-zip \
    -g $resourceGroupName \
	-n $publishFxAppName \
    -t 360 \
    --src publishfx.zip
	
# Deploy rbac app
az webapp deployment source config-zip \
    -g $resourceGroupName \
	-n $rbacFxAppName \
    -t 360 \
    --src rbacfx.zip

# Deploy partner app
az webapp deployment source config-zip \
    -g $resourceGroupName \
	-n $partnerFxAppName \
    -t 360 \
    --src partnerfx.zip
	
# Deploy routing app
az webapp deployment source config-zip \
    -g $resourceGroupName \
	-n $routingFxAppName \
    -t 360 \
    --src routingfx.zip

storageConnectionString=$(az storage account show-connection-string -g $resourceGroupName -n $storageName | ./jq -r '.connectionString')

sqlConnectionSring="Server=tcp:${sqlServerName}.database.windows.net,1433;Initial Catalog=${sqlDbName};Persist Security Info=False;User ID=${sqlUser};Password=${sqlPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

rbacFxUrl="https://${rbacFxAppName}.azurewebsites.net/api/"
publishFxUrl="https://${publishFxAppName}.azurewebsites.net/api/"
partnerFxUrl="https://${partnerFxAppName}.azurewebsites.net/api/"

rbacFxKey=$(az functionapp keys list -g $resourceGroupName -n $rbacFxAppName | ./jq -r '.functionKeys.default')
publishFxKey=$(az functionapp keys list -g $resourceGroupName -n $publishFxAppName | ./jq -r '.functionKeys.default')
partnerFxKey=$(az functionapp keys list -g $resourceGroupName -n $partnerFxAppName | ./jq -r '.functionKeys.default')

MSYS_NO_PATHCONV=1 az functionapp config appsettings set \
  --name $gatewayFxAppName \
  --resource-group $resourceGroupName \
  --settings "PARTNER_SERVICE_BASE_URL=${partnerFxUrl}" \
             "PARTNER_SERVICE_KEY=${partnerFxKey}" \
			 "RBAC_SERVICE_BASE_URL=${rbacFxUrl}" \
			 "RBAC_SERVICE_KEY=${rbacFxKey}" \
			 "PUBLISH_SERVICE_BASE_URL=${publishFxUrl}" \
			 "PUBLISH_SERVICE_KEY=${publishFxKey}"

MSYS_NO_PATHCONV=1 az functionapp config appsettings set \
  --name $publishFxAppName \
  --resource-group $resourceGroupName \
  --settings "SQL_CONNECTION_STRING=${sqlConnectionSring}" \
             "KEY_VAULT_NAME=${keyVaultName}" \
             "STORAGE_ACCOUNT_CONNECTION_STRING=${storageConnectionString}" 

MSYS_NO_PATHCONV=1 az functionapp config appsettings set \
  --name $partnerFxAppName \
  --resource-group $resourceGroupName \
  --settings "SQL_CONNECTION_STRING=${sqlConnectionSring}" \
             "KEY_VAULT_NAME=${keyVaultName}" 

MSYS_NO_PATHCONV=1 az functionapp config appsettings set \
  --name $rbacFxAppName \
  --resource-group $resourceGroupName \
  --settings "SQL_CONNECTION_STRING=${sqlConnectionSring}"

MSYS_NO_PATHCONV=1 az functionapp config appsettings set \
  --name $routingFxAppName \
  --resource-group $resourceGroupName \
  --settings "SQL_CONNECTION_STRING=${sqlConnectionSring}" \
             "KEY_VAULT_NAME=${keyVaultName}" \
             "STORAGE_ACCOUNT_CONNECTION_STRING=${storageConnectionString}" 
		   
		   
tokenIssuerUrl="https://sts.windows.net/${aadTenantId}/v2.0"
az webapp auth update  \
  -g $resourceGroupName \
  -n $gatewayFxAppName \
  --enabled true \
  --action LoginWithAzureActiveDirectory \
  --aad-allowed-token-audiences $aadClientId \
  --aad-client-id $aadClientId \
  --aad-client-secret $aadSecret \
  --aad-token-issuer-url $tokenIssuerUrl


az sql server firewall-rule create \
  -g $resourceGroupName \
  -s $sqlServerName \
  -n "all temp" \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 255.255.255.255

sqlcmd -S "tcp:${sqlServerName}.database.windows.net,1433" -U $sqlUser -P $sqlPassword -d $sqlDbName -i "lunadb.sql" -v adminUserName=$adminUserName -v adminUserId=$adminUserId

az sql server firewall-rule delete \
  -g $resourceGroupName \
  -s $sqlServerName \
  -n "all temp" \
  
read -p "Press enter to continue"