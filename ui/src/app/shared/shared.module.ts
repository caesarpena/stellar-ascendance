import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { FuseLoadingBarModule } from '@fuse/components/loading-bar';
import { MediaItemListViewModule } from './media-items-list/media-items-list.module';

@NgModule({
    declarations: [
    ],
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        MediaItemListViewModule
    ],
    exports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        FuseLoadingBarModule,
        MediaItemListViewModule,
    ],
})
export class SharedModule
{
}
