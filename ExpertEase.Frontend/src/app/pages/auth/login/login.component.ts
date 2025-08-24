import { Component, OnInit, NgZone } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { LoginDTO, SocialLoginDTO } from '../../../models/api.models';
import { FormField, dtoToFormFields } from '../../../models/form.models';
import { DynamicFormComponent } from '../../../shared/dynamic-form/dynamic-form.component';
import {environment} from '../../../../environments/environment';

declare global {
  interface Window {
    FB: any;
    fbAsyncInit: () => void;
  }
}

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DynamicFormComponent
  ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
  formFields: FormField[] = [];
  errorMessage: string | null = null;
  isGoogleLoading = false;
  isFacebookLoading = false;

  private readonly GOOGLE_CLIENT_ID = environment.googleClientId;
  private readonly FACEBOOK_APP_ID = environment.facebookAppId;

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private ngZone: NgZone
  ) {
    const dto: LoginDTO = {
      email: '',
      password: ''
    };

    this.formFields = dtoToFormFields(dto, {
      email: { type: 'email' },
      password: { type: 'password' }
    });
  }

  ngOnInit() {
    this.checkForOAuthError();
    this.loadFacebookSDK();
    console.log('Login component initialized');
    console.log('Google Client ID:', this.GOOGLE_CLIENT_ID);
    console.log('Current origin:', window.location.origin);
  }

  private checkForOAuthError() {
    // Check for OAuth errors from redirect
    this.route.queryParams.subscribe(params => {
      if (params['error']) {
        this.errorMessage = params['error'];
        // Clean URL
        this.router.navigate([], {
          relativeTo: this.route,
          queryParams: {},
          replaceUrl: true
        });
      }
    });
  }

  loginUser(data: { [key: string]: any }) {
    const loginData: LoginDTO = data as LoginDTO;

    this.authService.loginUser(loginData).subscribe({
      next: (res) => {
        console.log('Login successful:', res);
        this.router.navigate(['/home']);
      },
      error: (err) => {
        console.error('Login failed:', err);
        this.errorMessage = err.error?.errorMessage?.message || 'Eroare necunoscută.';
      }
    });
  }

  // Manual Google OAuth Flow
  signInWithGoogle() {
    console.log('Starting Google OAuth redirect flow');

    if (this.isGoogleLoading) return;

    this.isGoogleLoading = true;
    this.errorMessage = null;

    try {
      // Google OAuth parameters
      const redirectUri = window.location.origin + '/auth/google/callback';
      const scope = 'email profile';
      const responseType = 'code';
      const state = this.generateRandomState();

      console.log('=== OAUTH REDIRECT DEBUG ===');
      console.log('Client ID:', this.GOOGLE_CLIENT_ID);
      console.log('Redirect URI:', redirectUri);
      console.log('State:', state);
      console.log('============================');

      // Store state and current page for validation and return
      sessionStorage.setItem('google_oauth_state', state);
      sessionStorage.setItem('google_oauth_redirect', '/home');
      sessionStorage.setItem('google_oauth_type', 'login');
      sessionStorage.setItem('google_oauth_source', 'login'); // Remember where we came from

      // Create Google OAuth URL
      const params = new URLSearchParams({
        client_id: this.GOOGLE_CLIENT_ID,
        redirect_uri: redirectUri,
        scope: scope,
        response_type: responseType,
        state: state,
        access_type: 'offline',
        prompt: 'select_account',
        include_granted_scopes: 'true'
      });

      const googleAuthUrl = `https://accounts.google.com/oauth/authorize?${params.toString()}`;

      console.log('Redirecting to Google OAuth URL:', googleAuthUrl);

      // Direct redirect (no popup)
      window.location.href = googleAuthUrl;

    } catch (error) {
      this.isGoogleLoading = false;
      console.error('Error starting Google OAuth:', error);
      this.errorMessage = 'Eroare la inițializarea autentificării Google.';
    }
  }

  private openGooglePopup(url: string) {
    console.log('Opening Google popup...');

    const popup = window.open(
      url,
      'google-signin',
      'width=500,height=600,scrollbars=yes,resizable=yes,toolbar=no,menubar=no,location=no,directories=no,status=no'
    );

    if (!popup) {
      console.log('Popup blocked, trying redirect method');
      this.fallbackToRedirect(url);
      return;
    }

    // Monitor popup
    const checkClosed = setInterval(() => {
      try {
        if (popup.closed) {
          clearInterval(checkClosed);
          this.handlePopupClosed();
        }
      } catch (error) {
        // Cross-origin error when popup is still open
      }
    }, 1000);

    // Timeout after 5 minutes
    setTimeout(() => {
      if (!popup.closed) {
        popup.close();
        clearInterval(checkClosed);
        this.ngZone.run(() => {
          this.isGoogleLoading = false;
          this.errorMessage = 'Timp expirat pentru autentificarea Google.';
        });
      }
    }, 300000);
  }

  private handlePopupClosed() {
    this.ngZone.run(() => {
      console.log('Google popup closed, checking for auth code...');

      // Check if we got a code in localStorage (set by callback page)
      const authCode = localStorage.getItem('google_auth_code');
      const authState = localStorage.getItem('google_auth_state');
      const authError = localStorage.getItem('google_auth_error');
      const storedState = sessionStorage.getItem('google_oauth_state');

      if (authError) {
        // Clear error
        localStorage.removeItem('google_auth_error');
        this.isGoogleLoading = false;
        this.errorMessage = authError;
        return;
      }

      if (authCode && authState === storedState) {
        console.log('Auth code received, exchanging for token...');

        // Clear stored values
        localStorage.removeItem('google_auth_code');
        localStorage.removeItem('google_auth_state');
        sessionStorage.removeItem('google_oauth_state');

        // Exchange code for user info
        this.exchangeGoogleCode(authCode);
      } else {
        this.isGoogleLoading = false;
        if (!authCode) {
          this.errorMessage = 'Autentificarea Google a fost anulată.';
        } else {
          this.errorMessage = 'Eroare de securitate în autentificarea Google.';
        }
      }
    });
  }

  private fallbackToRedirect(url: string) {
    console.log('Using redirect method for Google OAuth');
    // Direct redirect as fallback
    window.location.href = url;
  }

  private exchangeGoogleCode(code: string) {
    console.log('Exchanging Google code for user info');

    const exchangeData = {
      code: code,
      provider: 'google',
      redirectUri: window.location.origin + '/auth/google/callback'
    };

    this.authService.exchangeOAuthCode(exchangeData).subscribe({
      next: (result) => {
        console.log('Google OAuth exchange successful:', result);
        this.isGoogleLoading = false;
        this.router.navigate(['/home']);
      },
      error: (err) => {
        console.error('Google OAuth exchange failed:', err);
        this.isGoogleLoading = false;
        this.errorMessage = err.error?.errorMessage?.message || 'Eroare la procesarea autentificării Google.';
      }
    });
  }

  // Facebook Sign-In (keeping original approach as it works better)
  private loadFacebookSDK() {
    if (typeof window === 'undefined') return;

    if (window.FB) {
      console.log('Facebook SDK already available');
      return;
    }

    console.log('Loading Facebook SDK...');

    window.fbAsyncInit = () => {
      window.FB.init({
        appId: this.FACEBOOK_APP_ID,
        cookie: true,
        xfbml: true,
        version: 'v18.0'
      });
      console.log('Facebook SDK initialized successfully');
    };

    const script = document.createElement('script');
    script.async = true;
    script.defer = true;
    script.crossOrigin = 'anonymous';
    script.src = 'https://connect.facebook.net/en_US/sdk.js';

    script.onerror = (error) => {
      console.error('Failed to load Facebook SDK:', error);
    };

    document.head.appendChild(script);
  }

  signInWithFacebook() {
    if (!window.FB) {
      this.errorMessage = 'Facebook SDK nu este disponibil. Reîncărcați pagina.';
      return;
    }

    if (this.isFacebookLoading) return;

    this.isFacebookLoading = true;
    this.errorMessage = null;

    window.FB.getLoginStatus((statusResponse: any) => {
      this.ngZone.run(() => {
        console.log('Facebook status:', statusResponse);

        if (statusResponse.status === 'connected') {
          this.handleFacebookResponse(statusResponse.authResponse.accessToken);
        } else {
          window.FB.login((loginResponse: any) => {
            this.ngZone.run(() => {
              this.isFacebookLoading = false;
              console.log('Facebook login response:', loginResponse);

              if (loginResponse.authResponse) {
                this.handleFacebookResponse(loginResponse.authResponse.accessToken);
              } else {
                console.error('Facebook login failed or cancelled:', loginResponse);
                this.errorMessage = 'Autentificarea Facebook a fost anulată sau a eșuat.';
              }
            });
          }, {
            scope: 'email,public_profile',
            return_scopes: true,
            auth_type: 'rerequest'
          });
        }
      });
    });
  }

  private handleFacebookResponse(accessToken: string) {
    console.log('Facebook token received');

    if (!accessToken) {
      this.isFacebookLoading = false;
      this.errorMessage = 'Nu s-a primit token-ul de la Facebook.';
      return;
    }

    const socialLoginData: SocialLoginDTO = {
      token: accessToken,
      provider: 'facebook'
    };

    this.authService.socialLogin(socialLoginData).subscribe({
      next: (result) => {
        console.log('Facebook social login successful:', result);
        this.isFacebookLoading = false;
        this.router.navigate(['/home']);
      },
      error: (err) => {
        console.error('Facebook social login failed:', err);
        this.isFacebookLoading = false;
        this.errorMessage = err.error?.errorMessage?.message || 'Eroare la autentificarea cu Facebook.';
      }
    });
  }

  // Utility methods
  private generateRandomState(): string {
    return Math.random().toString(36).substring(2, 15) +
      Math.random().toString(36).substring(2, 15);
  }

  retryGoogleSignIn() {
    this.isGoogleLoading = false;
    this.errorMessage = null;
    setTimeout(() => {
      this.signInWithGoogle();
    }, 100);
  }

  retryFacebookSignIn() {
    this.isFacebookLoading = false;
    this.errorMessage = null;

    if (window.FB) {
      window.FB.logout(() => {
        setTimeout(() => {
          this.signInWithFacebook();
        }, 100);
      });
    } else {
      setTimeout(() => {
        this.signInWithFacebook();
      }, 100);
    }
  }

  goToRegister(): void {
    this.router.navigate(['/register']);
  }
}
