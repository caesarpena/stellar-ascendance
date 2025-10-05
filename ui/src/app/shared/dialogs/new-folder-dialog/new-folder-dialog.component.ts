import { Component, Inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { UntypedFormBuilder, UntypedFormGroup, NgForm, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { NewFolderDialogData } from './new-folder-dialogData';

@Component({
    selector       : 'new-folder-dialog',
    templateUrl    : './new-folder-dialog.component.html',
    styleUrls  : ['./new-folder-dialog.component.scss']
})
export class NewFolderDialogComponent implements OnInit, OnDestroy {
    @ViewChild('newFolderNgForm') newFolderNgForm: NgForm;
    newFolderForm: UntypedFormGroup;

    constructor(private _formBuilder: UntypedFormBuilder,
        public dialogRef: MatDialogRef<NewFolderDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: NewFolderDialogData,
      ) {}

    ngOnInit(): void {
         // Create the form
         this.newFolderForm = this._formBuilder.group({
            folderName: [this.data.folderName, [Validators.required]],
        });
    }

    onNoClick(): void {
        this.dialogRef.close();
    }
    ngOnDestroy(): void {}
}
