import {Component, OnInit} from '@angular/core';
import {StripeAccountService} from '../../../services/stripe-account.service';
import {UserService} from '../../../services/user.service';
import {SpecialistProfileService} from '../../../services/specialist-profile.service';
import {AuthService} from '../../../services/auth.service';
import {Router, ActivatedRoute} from '@angular/router';
import {NgClass, NgIf} from '@angular/common';

@Component({
  selector: 'app-stripe-account',
  imports: [NgIf, NgClass],
  templateUrl: './stripe-account.component.html',
  styleUrl: './stripe-account.component.scss'
})
export class StripeAccountComponent implements OnInit {
  loading = false;
  isGeneratingLink = false;
  hasStripeAccount = false;
  accountComplete = false;
  stripeAccountId = '';
  errorMessage = '';
  successMessage = '';
  userProfile: any = null;

  constructor(
    private stripeAccountService: StripeAccountService,
    private userService: UserService,
    private specialistProfileService: SpecialistProfileService,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    this.checkReturnStatus();
    this.loadUserStripeAccount();
  }

  private checkReturnStatus() {
    this.route.queryParams.subscribe(params => {
      const status = params['status'];

      if (status === 'onboarding-complete') {
        this.successMessage = 'Configurarea contului Stripe a fost finalizată cu succes! Poți acum primi plăți de la clienți.';
        this.clearSuccessMessage();
      } else if (status === 'onboarding-refresh') {
        this.successMessage = 'Te rugăm să finalizezi configurarea contului Stripe pentru a putea primi plăți.';
        this.clearSuccessMessage();
      } else if (status === 'dashboard-complete') {
        this.successMessage = 'Setările contului au fost actualizate cu succes!';
        this.clearSuccessMessage();
      } else if (status === 'dashboard-refresh') {
        this.successMessage = 'Te rugăm să finalizezi actualizarea setărilor contului.';
        this.clearSuccessMessage();
      }

      // Clean URL after processing
      if (status) {
        this.router.navigate([], {
          relativeTo: this.route,
          queryParams: {},
          replaceUrl: true
        });
      }
    });
  }

  private clearSuccessMessage() {
    setTimeout(() => {
      this.successMessage = '';
    }, 8000); // Clear after 8 seconds
  }

  get accountStatusClass() {
    if (!this.hasStripeAccount) return 'not-configured';
    if (!this.accountComplete) return 'incomplete';
    return 'complete';
  }

  get accountStatusIcon() {
    if (!this.hasStripeAccount) return '⚠️';
    if (!this.accountComplete) return '⏳';
    return '✅';
  }

  get accountStatusTitle() {
    if (!this.hasStripeAccount) return 'Cont neconfigurat';
    if (!this.accountComplete) return 'Configurare incompletă';
    return 'Cont configurat complet';
  }

  get accountStatusDescription() {
    if (!this.hasStripeAccount) return 'Nu ai încă un cont Stripe configurat.';
    if (!this.accountComplete) return 'Completează configurarea pentru a putea primi plăți.';
    return 'Contul tău este configurat și poți primi plăți.';
  }

  async loadUserStripeAccount() {
    this.loading = true;
    this.errorMessage = '';

    try {
      const currentUserId = this.authService.getUserId();
      if (!currentUserId) {
        throw new Error('User ID not found');
      }

      console.log('Loading specialist profile for user:', currentUserId);

      // Get user's specialist profile to check for Stripe account
      this.specialistProfileService.getSpecialistProfile().subscribe({
        next: async (profileResponse) => {
          console.log('Specialist profile response:', profileResponse);

          if (profileResponse?.response && profileResponse.response) {
            this.userProfile = profileResponse.response;
            this.stripeAccountId = profileResponse.response.stripeAccountId || '';
            this.hasStripeAccount = !!this.stripeAccountId;

            // If we have a Stripe account ID, check its status
            if (this.stripeAccountId) {
              await this.checkStripeAccountStatus();
            } else {
              this.accountComplete = false;
            }

            console.log('User Stripe Account ID:', this.stripeAccountId);
            console.log('Has Stripe Account:', this.hasStripeAccount);
            console.log('Account Complete:', this.accountComplete);
          } else {
            console.log('No specialist profile found or response failed');
            this.hasStripeAccount = false;
            this.accountComplete = false;
            this.errorMessage = 'Nu ai încă un profil de specialist configurat.';
          }

          this.loading = false;
        },
        error: (error) => {
          console.error('Error loading Stripe account status:', error);
          this.errorMessage = 'Eroare la încărcarea statusului contului.';
          this.hasStripeAccount = false;
          this.accountComplete = false;
          this.loading = false;
        }
      });

    } catch (error: any) {
      console.error('Error in loadUserStripeAccount:', error);
      this.errorMessage = 'Eroare la încărcarea statusului contului.';
      this.hasStripeAccount = false;
      this.accountComplete = false;
      this.loading = false;
    }
  }

  private async checkStripeAccountStatus() {
    try {
      this.stripeAccountService.getAccountStatus(this.stripeAccountId).subscribe({
        next: (statusResponse) => {
          console.log('Stripe account status:', statusResponse);

          if (statusResponse?.response) {
            const status = statusResponse.response;
            this.accountComplete = status.isActive; // or use your own logic

            // You can also show more detailed status
            if (!status.chargesEnabled) {
              console.log('Charges not enabled yet');
            }
            if (!status.payoutsEnabled) {
              console.log('Payouts not enabled yet');
            }
            if (status.requirementsCurrentlyDue.length > 0) {
              console.log('Requirements currently due:', status.requirementsCurrentlyDue);
            }
          } else {
            console.error('Failed to get account status');
            this.accountComplete = false;
          }
        },
        error: (error) => {
          console.error('Error checking Stripe account status:', error);
          this.accountComplete = false;
        }
      });
    } catch (error) {
      console.error('Exception checking Stripe account status:', error);
      this.accountComplete = false;
    }
  }

  async activateStripeAccount() {
    if (!this.stripeAccountId) {
      this.errorMessage = 'Nu există un cont Stripe asociat cu profilul tău.';
      return;
    }

    this.isGeneratingLink = true;
    this.errorMessage = '';

    try {
      console.log('Generating onboarding link for account:', this.stripeAccountId);

      // Generate onboarding link for existing Stripe account
      this.stripeAccountService.generateOnboardingLink(this.stripeAccountId).subscribe({
        next: (linkResponse) => {
          console.log('Link response:', linkResponse);

          if (linkResponse?.response && linkResponse.response?.url) {
            // Redirect to Stripe onboarding
            console.log('Redirecting to:', linkResponse.response.url);
            window.location.href = linkResponse.response.url;
          } else {
            throw new Error(linkResponse?.errorMessage?.message || 'Eroare la generarea link-ului de configurare.');
          }
        },
        error: (error) => {
          console.error('Error generating onboarding link:', error);

          // More specific error handling
          if (error.status === 401) {
            this.errorMessage = 'Nu ești autentificat. Te rugăm să te conectezi din nou.';
          } else if (error.status === 403) {
            this.errorMessage = 'Nu ai permisiunea să accesezi această funcționalitate.';
          } else if (error.status === 404) {
            this.errorMessage = 'Contul Stripe nu a fost găsit.';
          } else {
            this.errorMessage = error.message || error.error?.message || 'Eroare la configurarea contului Stripe.';
          }

          this.isGeneratingLink = false;
        }
      });

    } catch (error: any) {
      console.error('Error in activateStripeAccount:', error);
      this.errorMessage = error.message || 'Eroare la configurarea contului Stripe.';
      this.isGeneratingLink = false;
    }
  }

  async openDashboard() {
    if (!this.stripeAccountId) {
      this.errorMessage = 'Nu există un cont Stripe asociat cu profilul tău.';
      return;
    }

    this.isGeneratingLink = true;
    this.errorMessage = '';

    try {
      console.log('Generating dashboard link for account:', this.stripeAccountId);

      // Generate dashboard link for existing Stripe account
      this.stripeAccountService.generateDashboardLink(this.stripeAccountId).subscribe({
        next: (linkResponse) => {
          console.log('Dashboard link response:', linkResponse);

          if (linkResponse?.response && linkResponse.response?.url) {
            // Redirect to Stripe dashboard
            console.log('Redirecting to dashboard:', linkResponse.response.url);
            window.location.href = linkResponse.response.url;
          } else {
            throw new Error(linkResponse?.errorMessage?.message || 'Eroare la generarea link-ului de dashboard.');
          }
        },
        error: (error) => {
          console.error('Error generating dashboard link:', error);

          // More specific error handling
          if (error.status === 401) {
            this.errorMessage = 'Nu ești autentificat. Te rugăm să te conectezi din nou.';
          } else if (error.status === 403) {
            this.errorMessage = 'Nu ai permisiunea să accesezi această funcționalitate.';
          } else if (error.status === 404) {
            this.errorMessage = 'Contul Stripe nu a fost găsit.';
          } else {
            this.errorMessage = error.message || error.error?.message || 'Eroare la accesarea dashboard-ului Stripe.';
          }

          this.isGeneratingLink = false;
        }
      });

    } catch (error: any) {
      console.error('Error in openDashboard:', error);
      this.errorMessage = error.message || 'Eroare la accesarea dashboard-ului Stripe.';
      this.isGeneratingLink = false;
    }
  }

  goToSpecialistProfile() {
    this.router.navigate(['/profile/specialist']);
  }

  goBack() {
    this.router.navigate(['/profile']);
  }
}
