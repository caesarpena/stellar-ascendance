import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';

@Component({
    selector       : 'confirm-delete-dialog',
    templateUrl    : './confirm-delete-dialog.component.html',
    styleUrls  : ['./confirm-delete-dialog.component.scss']
})
export class ConfirmDeleteDialogComponent implements OnInit, OnDestroy {

    constructor(public dialogRef: MatDialogRef<ConfirmDeleteDialogComponent>) {}

    ngOnInit(): void {
    }

    onNoClick(): void {
        this.dialogRef.close();
    }
    ngOnDestroy(): void {}
}
