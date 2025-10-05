import { Component, Input, OnDestroy, OnInit } from '@angular/core';

@Component({
    selector       : 'app-media-player',
    templateUrl    : './media-player.component.html',
    styleUrls  : ['./media-player.component.scss']
})
export class MediaPlayerComponent implements OnInit, OnDestroy {

    @Input() url: string;
    @Input() type: string;

    constructor(
      ) {}
    
    ngOnInit(): void {

    }

    ngOnDestroy(): void {}
}