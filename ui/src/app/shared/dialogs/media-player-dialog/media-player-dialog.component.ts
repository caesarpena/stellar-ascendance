import { Component, Inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, NgForm, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MediaPlayerDialogData } from './mediaPlayer-dialog-data';

@Component({
    selector       : 'media-player-dialog',
    templateUrl    : './media-player-dialog.component.html',
    styleUrls  : ['./media-player-dialog.component.scss']
})
export class MediaPlayerDialogComponent implements OnInit, OnDestroy {
    @ViewChild('newFolderNgForm') newFolderNgForm: NgForm;
    newFolderForm: FormGroup;

    constructor(private _formBuilder: FormBuilder,
        public dialogRef: MatDialogRef<MediaPlayerDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: MediaPlayerDialogData,
      ) {}
    
    ngOnInit(): void {
         // Create the form
         this.newFolderForm = this._formBuilder.group({
            url: [this.data.url, [Validators.required]],
            type: [this.data.type, [Validators.required]],
        });
    }

    onNoClick(): void {
        this.dialogRef.close();
    }
    ngOnDestroy(): void {}
}