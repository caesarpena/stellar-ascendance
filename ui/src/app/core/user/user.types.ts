export interface User
{
    id: string;
    firstName: string;
    lastName: string;
    phoneNumber?: string;
    userName: string;
    email: string;
    dob?: Date;
    avatar?: string;
    cover?: string;
    background?: string;
    userRoles: any;
    status?: string;
}

export interface UserAddress
{
    id: string;
    UserId: string;
    Country: string;
    State?: string;
    City: string;
    AddressLine1: string;
    AddressLine2?: string;
    Postcode?: string;
}