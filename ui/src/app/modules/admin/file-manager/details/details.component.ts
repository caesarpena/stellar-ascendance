import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit, ViewChild, ViewEncapsulation } from '@angular/core';
import { MatDrawerToggleResult } from '@angular/material/sidenav';
import { catchError, Observable, Subject, takeUntil, throwError } from 'rxjs';
import { FileManagerListComponent } from '../list/index';
import { FileManagerService } from '../file-manager.service';
import { IMediaItem, MediaItem } from '../file-manager.types';
import { MatDialog } from '@angular/material/dialog';
import { MediaPlayerDialogComponent } from 'app/shared/dialogs/media-player-dialog';
import { FuseConfirmationService } from '@fuse/services/confirmation';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, Router } from '@angular/router';
import { UntypedFormBuilder, UntypedFormGroup, NgForm, Validators } from '@angular/forms';

@Component({
    selector       : 'file-manager-details',
    templateUrl    : './details.component.html',
    styleUrls: ['./details.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class FileManagerDetailsComponent implements OnInit, OnDestroy
{
    @ViewChild('mediaItemNgForm') mediaItemNgForm: NgForm;
    mediaItemForm: UntypedFormGroup;
    mediaItem: MediaItem;
    isLoading: boolean = false;
    isEditMode: boolean = false;
    private _unsubscribeAll: Subject<any> = new Subject<any>();

    /**
     * Constructor
     */
    constructor(
        private _changeDetectorRef: ChangeDetectorRef,
        private _fileManagerListComponent: FileManagerListComponent,
        private _fileManagerService: FileManagerService,
        private _fuseConfirmationService: FuseConfirmationService,
        private _snackBar: MatSnackBar,
        private _router: Router,
        private _formBuilder: UntypedFormBuilder,
        private _activatedRoute: ActivatedRoute,
        public dialog: MatDialog,
    )
    {
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Lifecycle hooks
    // -----------------------------------------------------------------------------------------------------

    /**
     * On init
     */
    ngOnInit(): void
    {
        // Open the drawer
        this._fileManagerListComponent.matDrawer.open();

        // Get the item
        this._fileManagerService.item$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((mediaItem: MediaItem) => {

                // Open the drawer in case it is closed
                this._fileManagerListComponent.matDrawer.open();

                // Get the item
                this.mediaItem = mediaItem;

                // Mark for check
                this._changeDetectorRef.markForCheck();
            });

        this.mediaItemForm = this._formBuilder.group({
            name: [this.mediaItem.name, [Validators.required]],
            description: [this.mediaItem.description],
        });
    }

    openMediaPlayerDialog(): void {

        const dialogRef = this.dialog.open(MediaPlayerDialogComponent,
            {
                data: {
                    url: this.mediaItem.azureUrl,
                    type: this.mediaItem.mediaType
                }
            });
            dialogRef.afterClosed().subscribe(() => {

            });
    }

    startEditMode(): void {
        this.isEditMode = true;
    }
    stopEditMode(): void {
        this.isEditMode = false;
    }

    saveMediaItem(mediaItem: MediaItem): void {
        const currentDate = new Date();
        // mediaItem.name = this.mediaItemForm.value.name;
        // mediaItem.modifiedAt = currentDate;
        // mediaItem.description = this.mediaItemForm.value.description;
        const item: MediaItem = {
            // eslint-disable-next-line @typescript-eslint/naming-convention
            id: mediaItem.id,
            folderId: mediaItem.folderId,
            name: this.mediaItemForm.value.name,
            type: mediaItem.type,
            mediaType: mediaItem.mediaType,
            createdAt: mediaItem.createdAt,
            createdBy: mediaItem.createdBy,
            azureUrl: mediaItem.azureUrl,
            size: mediaItem.size,
            modifiedAt: currentDate,
            description: this.mediaItemForm.value.description,
        };
        this._fileManagerService.editMediaItems(item)
        .subscribe(
            () => {
                this.isLoading = true;
                    //trigger toast notification success
                    this._snackBar.open(mediaItem.type+' updated successfuly!', 'Close', {
                        horizontalPosition: 'end',
                        verticalPosition: 'top',
                        duration: 5000,
                    });
                    this.isLoading = false;
                    this.isEditMode = false;

                    this._fileManagerService.getMediaItemById(mediaItem.id)
                    .pipe(takeUntil(this._unsubscribeAll))
                    .subscribe((editedMediaItem: MediaItem) => {
                        // Get the item
                        this.mediaItem = editedMediaItem;
                    });

                    this._fileManagerService.getMediaItems(this._fileManagerService.mediaItemId).subscribe();
                    this._changeDetectorRef.markForCheck();
            },
            (error) => {
                    //stop spinner
                    this.isLoading = false;
                    //trigger toast notification Error
                    this._snackBar.open('Error: '+error.error.message, 'Close', {
                        announcementMessage: 'Error: '+error.error.message,
                        horizontalPosition: 'end',
                        verticalPosition: 'top',
                        duration: 5000,
                    });
            }
        );
    }
    isSaveDisabled(): boolean {
        if(this.mediaItemForm.value.description === this.mediaItem.description &&
            this.mediaItemForm.value.name === this.mediaItem.name){
            return true;
        }
    }


    /**
     * Delete the media item
     */
     deleteMediaItemDialog(mediaItem: MediaItem): void
     {
         // Open the confirmation dialog
         const confirmation = this._fuseConfirmationService.open({
             title  : 'Delete exercise',
             message: 'Are you sure you want to delete this '+ mediaItem.type + '? This action cannot be undone!',
             actions: {
                 confirm: {
                     label: 'Delete'
                 }
             }
         });
         // Subscribe to the confirmation dialog closed action
         confirmation.afterClosed().subscribe((result) => {

             // If the confirm button pressed...
             if ( result === 'confirmed' )
             {
                 this._fileManagerService.deleteMediaItem(mediaItem.id)
                     .subscribe(
                         () => {
                            this.isLoading = true;
                             //trigger toast notification success
                            this._snackBar.open(mediaItem.type+' deleted successfuly!', 'Close', {
                                horizontalPosition: 'end',
                                verticalPosition: 'top',
                                duration: 5000,
                            });
                            this.isLoading = false;
                            this._router.navigate(['../../'], {relativeTo: this._activatedRoute});
                            this._changeDetectorRef.markForCheck();
                         },
                         (error) => {
                             //stop spinner
                             this.isLoading = false;
                             //trigger toast notification Error
                             this._snackBar.open('An Error ocurred, please try again or contact the administrator', 'Close', {
                                 announcementMessage: 'An Error ocurred, please try again or contact the administrator',
                                 horizontalPosition: 'end',
                                 verticalPosition: 'top',
                                 duration: 5000,
                             });
                         }
                     );
             }
         });
     }

     callGetMediaItems(id: string): Observable<MediaItem[]>
     {
         return this._fileManagerService.getMediaItems(id);
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
        // refresh media items
        this._fileManagerService.getMediaItems(this._fileManagerService.mediaItemId).subscribe();
        // Unsubscribe from all subscriptions
        this._unsubscribeAll.next(null);
        this._unsubscribeAll.complete();
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Public methods
    // -----------------------------------------------------------------------------------------------------

    /**
     * Close the drawer
     */
    closeDrawer(): Promise<MatDrawerToggleResult>
    {
        return this._fileManagerListComponent.matDrawer.close();
    }

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
