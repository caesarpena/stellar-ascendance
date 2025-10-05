import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, map, Observable, ReplaySubject, tap } from 'rxjs';
import { User, UserAddress } from 'app/core/user/user.types';
import { API_UTILS } from '../utils/api.utils';

@Injectable({
    providedIn: 'root'
})
export class UserService
{
    private _user: ReplaySubject<User> = new ReplaySubject<User>(1);

    private _userAddress: BehaviorSubject<UserAddress> = new BehaviorSubject<UserAddress>(null);

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
     * Setter & getter for user
     *
     * @param value
     */

    get user$(): Observable<User>
    {
        return this._user.asObservable();
    }
    set user(value: User)
    {
        // Store the value
        this._user.next(value);
    }

    get userAddress$(): Observable<UserAddress>
    {
        return this._userAddress.asObservable();
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Public methods
    // -----------------------------------------------------------------------------------------------------

    /**
     * Get the current logged in user data
     */
    get(): Observable<User>
    {
        const apiUrl = API_UTILS.config.base+API_UTILS.config.account.userDetails;
        return this._httpClient.get<User>(apiUrl).pipe(
            tap((user) => {
                this._user.next(user);
            })
        );
    }

    /**
     *  Get a user's list of addresses
     */
    getUserAddress(Id: string): Observable<UserAddress>
    {
        const apiUrl = API_UTILS.config.base+API_UTILS.config.user.getUserAddress;
        // eslint-disable-next-line @typescript-eslint/naming-convention
        const headers = { 'Authorization' : 'Bearer '+ localStorage.getItem('accessToken') };
        const options = {headers, params: {Id}};
        return this._httpClient.get<UserAddress>(apiUrl, options).pipe(
            tap((address) => {
                this._userAddress.next(address);
            })
        );
    }

    /**
     * Update the user
     *
     * @param user
     */
    update(user: User): Observable<any>
    {
        return this._httpClient.patch<User>('api/common/user', {user}).pipe(
            map((response) => {
                this._user.next(response);
            })
        );
    }
}
