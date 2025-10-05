import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSidenavModule } from '@angular/material/sidenav';
import {MatDialogModule} from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MediaItemListViewComponent, MediaItemsService } from './index';
import { ContextMenuModule } from '@perfectmemory/ngx-contextmenu';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import {MatProgressSpinnerModule} from '@angular/material/progress-spinner';
import { MediaPlayerComponent, MediaPlayerModule } from 'app/shared/media-player';
import { CommonModule } from '@angular/common';

@NgModule({
    declarations: [
        MediaItemListViewComponent
    ],
    imports     : [
        CommonModule,
        MatButtonModule,
        MatIconModule,
        MatDialogModule,
        MatSidenavModule,
        MatTooltipModule,
        MatFormFieldModule,
        MatInputModule,
        MatProgressSpinnerModule,
        MatSnackBarModule,
        MediaPlayerModule,
        ContextMenuModule.forRoot()
    ],
    exports: [
        MediaItemListViewComponent,
    ],
    providers: [
        MediaItemsService
    ]
})
export class MediaItemListViewModule
{
}
