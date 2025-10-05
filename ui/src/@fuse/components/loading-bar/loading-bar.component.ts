import { AfterContentInit, Component, EmbeddedViewRef, Input, OnChanges, OnDestroy, OnInit, SimpleChanges, ViewEncapsulation } from '@angular/core';
import { coerceBooleanProperty } from '@angular/cdk/coercion';
import { Subject, takeUntil } from 'rxjs';
import { FuseLoadingService } from '@fuse/services/loading';

@Component({
    selector     : 'fuse-loading-bar',
    templateUrl  : './loading-bar.component.html',
    styleUrls    : ['./loading-bar.component.scss'],
    encapsulation: ViewEncapsulation.None,
    exportAs     : 'fuseLoadingBar'
})
export class FuseLoadingBarComponent implements OnChanges, OnInit, OnDestroy
{
    @Input() show: boolean = false;
    @Input() autoMode: boolean = false;
    mode: 'determinate' | 'indeterminate';
    progress: number = 0;
    private _unsubscribeAll: Subject<any> = new Subject<any>();
    private _viewRef: EmbeddedViewRef<any>;

    /**
     * Constructor
     */
    constructor(private _fuseLoadingService: FuseLoadingService)
    {
        // Subscribe to the service
        this._fuseLoadingService.mode$
        .pipe(takeUntil(this._unsubscribeAll))
        .subscribe((value) => {
            this.mode = value;
        });

        this._fuseLoadingService.progress$
        .pipe(takeUntil(this._unsubscribeAll))
        .subscribe((value) => {
            this.progress = value;
        });

        this._fuseLoadingService.show$
        .pipe(takeUntil(this._unsubscribeAll))
        .subscribe((value) => {
            this.show = value;
        });
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Lifecycle hooks
    // -----------------------------------------------------------------------------------------------------

    /**
     * On changes
     *
     * @param changes
     */
    ngOnChanges(changes: SimpleChanges): void
    {
        // Auto mode
        if ( 'autoMode' in changes )
        {
            // Set the auto mode in the service
            this._fuseLoadingService.setAutoMode(coerceBooleanProperty(changes.autoMode.currentValue));
        }
        this._viewRef.detectChanges();
    }

    /**
     * On init
     */
    ngOnInit(): void
    {


    }

    /**
     * On destroy
     */
    ngOnDestroy(): void
    {
        // Unsubscribe from all subscriptions
        this._unsubscribeAll.next(null);
        this._unsubscribeAll.complete();
    }
}
