import { CommonModule } from '@angular/common';
import {NgModule} from '@angular/core';
import { MediaPlayerComponent, MediaPlayerModule } from 'app/shared/media-player';

@NgModule({
    declarations: [
    ],
    imports: [
        CommonModule,
        MediaPlayerModule
    ],
    providers: [],
    bootstrap: []
})
export class MediaPlayerDialogModule {
}