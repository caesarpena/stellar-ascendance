/* eslint-disable @typescript-eslint/naming-convention */
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, empty, map, Observable, of, switchMap, take, tap, throwError } from 'rxjs';
import { IMediaItem, MediaItem, MediaItems } from './file-manager.types';
import { API_UTILS } from 'app/core/utils/api.utils';

@Injectable({
    providedIn: 'root'
})
export class FileManagerService
{
    mediaItemId: string = null;
    // Private
    private _mediaItem: BehaviorSubject<MediaItem | null> = new BehaviorSubject(null);
    private _mediaItems: BehaviorSubject<MediaItems | null> = new BehaviorSubject(null);

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
    get items$(): Observable<MediaItems>
    {
        return this._mediaItems.asObservable();
    }

    /**
     * Getter for item
     */
    get item$(): Observable<MediaItem>
    {
        return this._mediaItem.asObservable();
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
      * Create Media Item
      *
      * @param MediaItem
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

    getMediaItems(id: string | null = null): Observable<MediaItem[]>
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
     * Get items
     */

     getMediaItemById(id: string): Observable<MediaItem>
    {
        const apiUrl = API_UTILS.config.base+API_UTILS.config.mediaItems.getMediaItemById;
        const headers = {
            'Authorization': 'Bearer '+ localStorage.getItem('accessToken')
        };
        const options = {headers, params: {id}};

        return this._httpClient.get<MediaItems>(apiUrl, options).pipe(
            tap((response: any) => {
                this._mediaItem.next(response);
            })
        );
    }

    /**
     * edit item
     */

    editMediaItems(mediaItem: MediaItem): Observable<MediaItem[]>
    {
        const apiUrl = API_UTILS.config.base+API_UTILS.config.mediaItems.patchMediaItem;
        const headers = {
            'Authorization': 'Bearer '+ localStorage.getItem('accessToken')
        };
        return this._httpClient.patch(apiUrl, mediaItem, {headers}).pipe(
            tap((response: any) => {
                this._mediaItems.next(response);
            })
        );
    }

    /**
     * Delete exercise
     *
     * @param exerciseID
     */
    deleteMediaItem(Id: string): Observable<MediaItem>
    {
        const apiUrl = API_UTILS.config.base+API_UTILS.config.mediaItems.deleteMediaItem;
        const headers = {
            'Authorization': 'Bearer '+ localStorage.getItem('accessToken')
        };
        const options = {headers, params: {Id}};
        return this._httpClient.delete(apiUrl, options).pipe(
            switchMap((response: any) => of(response))
        );
    }
}
