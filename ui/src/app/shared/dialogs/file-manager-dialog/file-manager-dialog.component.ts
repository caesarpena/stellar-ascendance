import { Component, Inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, NgForm, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MediaItemsService } from 'app/shared/media-items-list/media-items.service';
import { FileManagerDialogData } from './file-manager-dialog-dialogData';

@Component({
    selector       : 'file-manager-dialog',
    templateUrl    : './file-manager-dialog.component.html',
    styleUrls  : ['./file-manager-dialog.component.scss']
})
export class FileManagerDialogComponent implements OnInit, OnDestroy {
    // @ViewChild('createRoutineNgForm') createRoutineNgForm: NgForm;
    createRoutineForm: FormGroup;

    constructor(private _formBuilder: FormBuilder,
        public dialogRef: MatDialogRef<FileManagerDialogComponent>,
        // @Inject(MAT_DIALOG_DATA) public data: CreateRoutineDialogData,
      ) {}

    ngOnInit(): void {
         // Create the form
         this.createRoutineForm = this._formBuilder.group({
            routineName: ['', [Validators.required]],
        });
    }

    onNoClick(): void {
        this.dialogRef.close();
    }
    ngOnDestroy(): void {}
}
