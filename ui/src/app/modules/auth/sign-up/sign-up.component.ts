import { Component, OnInit, ViewChild, ViewEncapsulation } from '@angular/core';
import { UntypedFormBuilder, UntypedFormGroup, NgForm, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { fuseAnimations } from '@fuse/animations';
import { FuseAlertType } from '@fuse/components/alert';
import { AuthService } from 'app/core/auth/auth.service';

@Component({
    selector     : 'auth-sign-up',
    templateUrl  : './sign-up.component.html',
    encapsulation: ViewEncapsulation.None,
    animations   : fuseAnimations
})
export class AuthSignUpComponent implements OnInit
{
    @ViewChild('signUpNgForm') signUpNgForm: NgForm;

    alert: { type: FuseAlertType; message: string } = {
        type   : 'success',
        message: ''
    };
    signUpForm: UntypedFormGroup;
    showAlert: boolean = false;
    devFieldArray: Array<string> = [
        'Bodybuilding coach',
        'Swimming instructor',
        'Running instructor',
        'Recovery',
        'Nutrition',
        'Personal trainer',
        'Injury prevention specialist',
        'Yoga instructor',
        'Pilates instructor',
        'Aerobics instructor',
        'Biking instructor'
    ];

    /**
     * Constructor
     */
    constructor(
        private _authService: AuthService,
        private _formBuilder: UntypedFormBuilder,
        private _router: Router
    )
    {
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Lifecycle hooks
    // -----------------------------------------------------------------------------------------------------

    /**
     * On init
     */
    ngOnInit(): void
    {
        // Create the form
        this.signUpForm = this._formBuilder.group({
                name      : ['', Validators.required],
                lastname  : ['', Validators.required],
                email     : ['', [Validators.required, Validators.email]],
                password  : ['', Validators.required],
                devField  : ['', Validators.required],
                agreements: ['', Validators.requiredTrue]
            }
        );
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Public methods
    // -----------------------------------------------------------------------------------------------------

    /**
     * Sign up
     */
    signUp(): void
    {
        // Do nothing if the form is invalid
        if ( this.signUpForm.invalid )
        {
            return;
        }

        // Disable the form
        this.signUpForm.disable();

        // Hide the alert
        this.showAlert = false;
    }
}
