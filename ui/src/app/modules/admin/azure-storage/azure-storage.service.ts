import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, empty, map, Observable, of, switchMap, take, tap, throwError } from 'rxjs';
import { API_UTILS } from 'app/core/utils/api.utils';

@Injectable({
    providedIn: 'root'
})
export class FileManagerService
{
    // Private
    clientId: string = process.env.AZURE_CLIENT_ID as string;
    secret: string = process.env.AZURE_CLIENT_SECRET as string;
    subscriptionId: string = process.env.AZURE_SUBSCRIPTION_ID as string;
    resourceGroup: string = process.env.AZURE_RESOURCE_GROUP as string;
    accountName: string = process.env.AZURE_MEDIA_SERVICES_ACCOUNT_NAME as string;
    /**
     * Constructor
     */
    constructor(private _httpClient: HttpClient)
    {
    }

    /**
     * Create container/asset
     */
    // azureCreateContainer() {
    //     const credential = new DefaultAzureCredential();

    //     let mediaServicesClient =  new AzureMediaServices(credential, this.subscriptionId)
      
    //     // List Assets in Account
    //     console.log("Listing assets in account:")
      
    //     let assetName = "MyCustomAssetName";
    //     let storageContainerName = "mycustomcontainername"; // Lower case, numbers and dashes are ok. Check MSDN for more information about valid container naming
      
    //     console.log(`Creating a new Asset with the name : ${assetName} in storage container ${storageContainerName}`);
      
    //     let asset = await mediaServicesClient.assets.createOrUpdate(this.resourceGroup, this.accountName, assetName, {
    //       container:storageContainerName,
    //       alternateId: "MyCustomIdentifier",
    //       description: "my description",
    //       // storageAccountName: ""  // This is optional, if you have more than one storage account connected to the AMS account, you can specify which account to use
    //     }) 
      
    //     console.log(`Asset created!`);
      
    //     console.log(`This Asset is in storage account : ${asset.storageAccountName} in the container: ${asset.container}`);
      
    //     console.log('Deleting Asset');
    //     await mediaServicesClient.assets.delete(this.resourceGroup, this.accountName, assetName);
    //     console.log(`Asset is now deleted`);
    //   }
      
    //   main().catch((err) => {
          
    //     console.error("Error running sample:", err.message);
    //     console.error (`Error code: ${err.code}`);
      
    //     if (err.name == 'RestError'){
    //         // REST API Error message
    //         console.error("Error request:\n\n", err.request);
    //     }
    // }

}