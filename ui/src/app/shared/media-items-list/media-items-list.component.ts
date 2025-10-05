import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, Input, OnDestroy, OnInit, ViewChild, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatDrawer } from '@angular/material/sidenav';
import { Subject, takeUntil } from 'rxjs';
import { FuseMediaWatcherService } from '@fuse/services/media-watcher';
import { ContextMenuComponent, ContextMenuService } from '@perfectmemory/ngx-contextmenu';
import { MatDialog } from '@angular/material/dialog';
import { UntypedFormGroup } from '@angular/forms';
import {
    MatSnackBar,
    MatSnackBarHorizontalPosition,
    MatSnackBarVerticalPosition,
  } from '@angular/material/snack-bar';
import { NewFolderDialogComponent } from 'app/shared/dialogs/new-folder-dialog';
import { IMediaItem, MediaItem, MediaItems } from 'app/modules/admin/file-manager/file-manager.types';
import { MediaItemsService } from './media-items.service';
import { FileManagerService } from 'app/modules/admin/file-manager';
@Component({
    selector       : 'media-item-list-view',
    templateUrl    : './media-items-list.component.html',
    encapsulation  : ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MediaItemListViewComponent implements OnInit, OnDestroy
{
    @ViewChild('matDrawer', {static: true}) matDrawer: MatDrawer;
    @ViewChild('itemList', {static: false}) itemList: ElementRef;
    @ViewChild('fileInput', {static: false}) fileInput: ElementRef;

    // @ViewChild(ContextMenuComponent) public basicMenu: ContextMenuComponent;
    @Input() layout;
    @Input() isRouterLink: boolean = false; //routerLink

    drawerMode: 'side' | 'over';
    selectedMediaItem: MediaItem;
    selectedIntroVideo: MediaItem;
    mediaItems: MediaItems;
    path = [];
    horizontalPosition: MatSnackBarHorizontalPosition = 'end';
    verticalPosition: MatSnackBarVerticalPosition = 'top';
    isLoading: boolean = false;
    private _unsubscribeAll: Subject<any> = new Subject<any>();

    /**
     * Constructor
     */
    constructor(
        private _activatedRoute: ActivatedRoute,
        private _changeDetectorRef: ChangeDetectorRef,
        private _router: Router,
        private _mediaItemsService: MediaItemsService,
        private _fuseMediaWatcherService: FuseMediaWatcherService,
        public dialog: MatDialog,
        private _snackBar: MatSnackBar

    )
    {
        this._mediaItemsService.getMediaItems().subscribe();
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Lifecycle hooks
    // -----------------------------------------------------------------------------------------------------

    /**
     * On init
     */
    ngOnInit(): void
    {

        // // Get the items
        this._mediaItemsService.mediaItems$
        .pipe(takeUntil(this._unsubscribeAll))
        .subscribe((items: MediaItems) => {
            this.mediaItems = items;
            // Mark for check
            this._changeDetectorRef.markForCheck();
        });
    }

    itemClick(item: MediaItem): void {
        this._mediaItemsService.getMediaItems(item.id, item.folderId).subscribe();
    }

    selectFile(item: MediaItem): void {
        this._mediaItemsService.selectedMediaItem(item);
    }

    routeToHome(): void {
        this._mediaItemsService.getMediaItems().subscribe();
    }

    openFileDialog(): void {
        this.fileInput.nativeElement.click()
    }

    getfile(files: FormData): void {
        const formData = new FormData();
        if (files[0]) {
            const isFile = this.mediaItems.files.some(e => e.name === files[0].name.toString());
            if(isFile) {
                this._snackBar.open('Error: A file with the same same already exist in this directory', 'Close', {
                    horizontalPosition: this.horizontalPosition,
                    verticalPosition: this.verticalPosition,
                    duration: 5000,
                });
                return;
            }
            if(files[0].size >= 134217728) {
                this._snackBar.open('Error: The file has exceeded the size limit of 128 MB \n TIP: please consider'+
                'uploading the content in different videos of no more than 30 seconds each',
                'Close', {
                    horizontalPosition: this.horizontalPosition,
                    verticalPosition: this.verticalPosition,
                    duration: 5000,
                });
                return;
            }
            if(files[0].name.toString().length > 55) {
                this._snackBar.open('Error: The file name should not exceed 55 characters',
                'Close', {
                    horizontalPosition: this.horizontalPosition,
                    verticalPosition: this.verticalPosition,
                    duration: 5000,
                });
                return;
            }
            formData.append(files[0].name, files[0]);
            
            this._mediaItemsService.uploadMediaItem(formData)
            .subscribe(
                (azureUrl) => {
                    this.createNewItem(
                        files[0].name.toString(),
                        'file',
                        files[0].type,
                        files[0].size.toString(),
                        new Date(files[0].lastModifiedDate.toString()),
                        azureUrl.toReturn,
                    );
                },
                (error) => {
                        //stop spinner
                    this.isLoading = false;
                    //trigger toast notification Error
                    this._snackBar.open('Error: '+error.error.message, 'Close', {
                        announcementMessage: error,
                        horizontalPosition: this.horizontalPosition,
                        verticalPosition: this.verticalPosition,
                        duration: 5000,
                    });
                }
            );
        }
    }

    openFolderDialog(): void {
        let folderName = 'new folder';
        let isFolder;
        const folderNumber = '';
        let index = 0;

        do {
            isFolder = this.mediaItems.folders.find(e => e.name.includes(folderName));
            if(isFolder) {
                index = index+1;
                folderName = 'new folder '+index;
            }

        }while(isFolder);

        const dialogRef = this.dialog.open(NewFolderDialogComponent,
            {
                data: {
                    folderName: folderName
                }
            });
            dialogRef.afterClosed().subscribe((result) => {
            const form: UntypedFormGroup = result;
            if(form.value) {
            this.createNewItem(
                form.value.folderName,
                'folder',
                null,
                '0',
                new Date(),
                null);
            }
        });
    }

    /**
     *  Create new folder api call
     */
    createNewItem(name: string, type: string, mediaType: string, size: string, modifiedAt: Date, azureUrl): void
    {
        this.isLoading = true;
        const folderId = this._mediaItemsService.mediaItemId;
        const currentDate = new Date();
        const item: IMediaItem = {
            // eslint-disable-next-line @typescript-eslint/naming-convention
            id: '',
            folderId: folderId,
            name: name,
            createdAt: currentDate,
            modifiedAt: currentDate,
            size: size,
            type: type,
            mediaType: mediaType,
            description: null,
            azureUrl: azureUrl
        };
        // Create item
        this._mediaItemsService.createMediaItem(item)
        .subscribe(
            () => {
                // refresh items
                this._mediaItemsService.getMediaItems(folderId)
                .pipe(takeUntil(this._unsubscribeAll))
                .subscribe((items: any) => {
                    this.mediaItems = items;
                    this._changeDetectorRef.markForCheck();
                });

                this.isLoading = false;
                //trigger toast notification success
                this._snackBar.open(type+' created successfuly!', 'Close', {
                    horizontalPosition: this.horizontalPosition,
                    verticalPosition: this.verticalPosition,
                    duration: 5000,
                });
            },
            (error) => {
                    //stop spinner
                this.isLoading = false;
                //trigger toast notification Error
                this._snackBar.open('Error: ', 'Close', {
                    announcementMessage: error.error.message,
                    horizontalPosition: this.horizontalPosition,
                    verticalPosition: this.verticalPosition,
                    duration: 5000,
                });
            }
        );
    }

    bytesToMiB(bytes: string): number {
        const mebibytes = +bytes / Math.pow(1024, 2);
        return parseFloat(mebibytes.toFixed(2));
    }
    /**
     * On destroy
     */
    ngOnDestroy(): void
    {
        // Unsubscribe from all subscriptions
        this._unsubscribeAll.next(null);
        this._unsubscribeAll.complete();
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Public methods
    // -----------------------------------------------------------------------------------------------------

    /**
     * Track by function for ngFor loops
     *
     * @param index
     * @param item
     */
    trackByFn(index: number, item: any): any
    {
        return item.id || index;
    }
}
