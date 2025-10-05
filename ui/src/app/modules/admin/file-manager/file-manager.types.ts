export interface MediaItems
{
    folders: MediaItem[];
    files: MediaItem[];
    path: any[];
}

export interface MediaItem
{
    id?: string;
    folderId?: string;
    name?: string;
    createdBy?: string;
    createdAt?: Date;
    modifiedAt?: Date;
    size?: string;
    type?: string;
    mediaType?: string;
    description?: string | null;
    azureUrl?: string | null;
    thumbnailUrl?: string | null;
}

export interface IMediaItem
{
    id?: string;
    folderId?: string;
    name?: string;
    createdAt?: Date;
    modifiedAt?: Date;
    size?: string;
    type?: string;
    mediaType?: string;
    description?: string | null;
    azureUrl?: string | null;
    thumbnailUrl?: string | null;
}
