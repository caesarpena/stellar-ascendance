import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSidenavModule } from '@angular/material/sidenav';
import {MatDialogModule} from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FileManagerComponent, FileManagerDetailsComponent, FileManagerListComponent, MediaItemListComponent } from './index';
import { fileManagerRoutes } from './file-manager.routing';
import { SharedModule } from '../../../shared/shared.module';
import { ContextMenuModule } from '@perfectmemory/ngx-contextmenu';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import {MatProgressSpinnerModule} from '@angular/material/progress-spinner';
import { NewFolderDialogComponent } from 'app/shared/dialogs/new-folder-dialog';
import { MediaPlayerDialogComponent } from 'app/shared/dialogs/media-player-dialog';
import { MediaPlayerComponent, MediaPlayerModule } from 'app/shared/media-player';
import { MatMenuModule } from '@angular/material/menu';

@NgModule({
    declarations: [
        FileManagerComponent,
        FileManagerDetailsComponent,
        FileManagerListComponent,
        MediaItemListComponent,
        NewFolderDialogComponent,
        MediaPlayerDialogComponent
    ],
    imports     : [
        RouterModule.forChild(fileManagerRoutes),
        SharedModule,
        MatButtonModule,
        MatIconModule,
        MatDialogModule,
        MatSidenavModule,
        MatTooltipModule,
        MatMenuModule,
        MatFormFieldModule,
        MatInputModule,
        MatProgressSpinnerModule,
        MatSnackBarModule,
        MediaPlayerModule,
        ContextMenuModule.forRoot()
    ],
    exports: [
        MediaItemListComponent,
    ]
})
export class FileManagerModule
{
}
