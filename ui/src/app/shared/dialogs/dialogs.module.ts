import {NgModule} from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { SharedModule } from '../shared.module';
import { ConfirmDeleteDialogComponent } from './confirm-delete-dialog';
import { FileManagerDialogComponent } from './file-manager-dialog';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import {MatTooltipModule} from '@angular/material/tooltip';
import {MatListModule} from '@angular/material/list';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatMenuModule } from '@angular/material/menu';
@NgModule({
    declarations: [
        FileManagerDialogComponent,
        ConfirmDeleteDialogComponent,
    ],
    imports: [
        SharedModule,
        ReactiveFormsModule,
        FormsModule,
        MatButtonModule,
        MatIconModule,
        MatDialogModule,
        MatFormFieldModule,
        MatSelectModule,
        MatInputModule,
        MatSlideToggleModule,
        MatTooltipModule,
        MatListModule,
        MatDatepickerModule,
        MatMenuModule
    ],
    exports: [
    ],
    bootstrap: []
})
export class DialogsModule {
}
