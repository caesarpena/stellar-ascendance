/* eslint-disable @typescript-eslint/naming-convention */
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, empty, map, Observable, of, switchMap, take, tap, throwError } from 'rxjs';
import { API_UTILS } from 'app/core/utils/api.utils';
import { MediaItem, MediaItems } from 'app/modules/admin/file-manager/file-manager.types';

@Injectable({
    providedIn: 'root'
})
export class MediaItemsService
{

    mediaItemId: string = '';
    // Private
    private _mediaItem: BehaviorSubject<MediaItem | null> = new BehaviorSubject(null);
    private _mediaItems: BehaviorSubject<MediaItems | null> = new BehaviorSubject(null);
    private _selectedMediaItem: BehaviorSubject<MediaItem | null> = new BehaviorSubject(null);


    /**
     * Constructor
     */
    constructor(private _httpClient: HttpClient)
    {
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Accessors
    // -----------------------------------------------------------------------------------------------------

    /**
     * Getter for items
     */
    get mediaItems$(): Observable<MediaItems>
    {
        return this._mediaItems.asObservable();
    }

    /**
     * Getter for item
     */
    get mediaItem$(): Observable<MediaItem>
    {
        return this._mediaItem.asObservable();
    }

    /**
     * Getter for item
     */
     get selectedMediaItem$(): Observable<MediaItem>
     {
         return this._selectedMediaItem.asObservable();
     }

    _getHeaders(): HttpHeaders {
        const headers = new HttpHeaders();
        headers.append('content-type', 'x-www-form-urlencoded');
        headers.append('Authorization', 'Bearer '+ localStorage.getItem('accessToken'));

        return headers;
     }

    // -----------------------------------------------------------------------------------------------------
    // @ Public methods
    // -----------------------------------------------------------------------------------------------------
    /**
     * Upload file to azure
     *
     * @param formData
     */
    uploadMediaItem(formData: FormData): Observable<any> {
        const apiUrl = API_UTILS.config.base+API_UTILS.config.uploadEncodeAndStreamFiles.uploadFile;
        const headers = this._getHeaders();

        return this._httpClient.post<{ path: string }>(
            apiUrl,
            formData,
            {headers: headers}
        ).pipe(
            switchMap((response: any) => of(response))
        );
      }


     /**
      * Create New Folder
      *
      * @param folder
      */
    createMediaItem(item: any): Observable<any>
    {
        const apiUrl = API_UTILS.config.base+API_UTILS.config.mediaItems.createMediaItem;
        // eslint-disable-next-line @typescript-eslint/naming-convention
        const headers = { 'Authorization': 'Bearer '+ localStorage.getItem('accessToken') };

        return this._httpClient.post(apiUrl, item, { headers }).pipe(
            switchMap((response: any) => of(response))
        );
    }

    /**
     * Get items
     */

    // getMediaItems(id: string | null = null, folderId: string | null = null): void
    // {
    //     const apiUrl = API_UTILS.config.base+API_UTILS.config.mediaItems.getMediaItems;
    //     const headers = {
    //         'Authorization': 'Bearer '+ localStorage.getItem('accessToken')
    //       };
    //     const options = id? {headers, params: {id, folderId}} : {headers, params: {}};

    //     this._httpClient.get<MediaItems>(apiUrl, options).subscribe((response) => {
    //         this._mediaItems.next(response);
    //       });
    // }

    /**
     * Get items
     */

    getMediaItems(id: string | null = null, folderId: string | null = null): Observable<MediaItem[]>
    {
        const apiUrl = API_UTILS.config.base+API_UTILS.config.mediaItems.getMediaItems;
        const headers = {
            'Authorization': 'Bearer '+ localStorage.getItem('accessToken')
        };
        this.mediaItemId = id;
        const options = id? {headers, params: {id}} : {headers, params: {}};

        return this._httpClient.get<MediaItems>(apiUrl, options).pipe(
            tap((response: any) => {
                this._mediaItems.next(response);
            })
        );
    }

    /**
     * Refresh items
     */
    refreshMediaItems(): Observable<MediaItems>
    {
        const apiUrl = API_UTILS.config.base+API_UTILS.config.mediaItems.getMediaItems;
        const headers = { 'Authorization': 'Bearer '+ localStorage.getItem('accessToken') };
        const folderId = this.mediaItemId;
        const options = folderId? {headers, params: {folderId}} : {headers, params: {}};
        return this._httpClient.get<MediaItems>(apiUrl, options).pipe(
            switchMap((response: MediaItems) => {
                this._mediaItems.next(response);
                return of(response);
            })
        );
    }

    /**
     * Get item by id
     */
    getMediaItemById(Id: string): Observable<MediaItem>
    {
        const apiUrl = API_UTILS.config.base+API_UTILS.config.mediaItems.getMediaItemById;
        const headers = {
            'Authorization': 'Bearer '+ localStorage.getItem('accessToken')
        };
        const options = {headers, params: {Id}};

        return this._httpClient.get(apiUrl, options).pipe(
            tap((response: any) => {
                this._mediaItem.next(response);
            })
        );
    }

    selectedMediaItem(mediaItem: MediaItem): void
    {
        this._selectedMediaItem.next(mediaItem);
    }

    /**
     * edit item
     */

     editMediaItems(folderId: string): Observable<MediaItem[]>
     {
         const apiUrl = API_UTILS.config.base+API_UTILS.config.mediaItems.getMediaItems;
         const headers = {
             'Authorization': 'Bearer '+ localStorage.getItem('accessToken')
           };
         const options = {headers, params: {folderId}};

         return this._httpClient.post(apiUrl, options).pipe(
             tap((response: any) => {
                 this._mediaItems.next(response);
             })
         );
     }


}
