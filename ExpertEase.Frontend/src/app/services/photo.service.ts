import { Injectable } from '@angular/core';
import {AuthService} from './auth.service';
import {HttpClient, HttpHeaders} from '@angular/common/http';
import {PortfolioPictureAddDTO, RequestResponse} from '../models/api.models';
import {Observable} from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class PhotoService {
  private readonly baseUrl = `http://localhost:5241/api/Photo`
  constructor(private readonly http: HttpClient, private readonly authService: AuthService) { }

  addProfilePicture(file: File) {
    const formData = new FormData();
    formData.append('file', file);
    const token = this.authService.getToken();
    const headers = new HttpHeaders({
      Authorization: `Bearer ${token}`
    });

    return this.http.post(`${this.baseUrl}/AddProfilePicture`, formData, { headers });
  }

  updateProfilePicture(file: File){
    const formData = new FormData();
    formData.append('file', file);
    const token = this.authService.getToken();
    const headers = new HttpHeaders({
      Authorization: `Bearer ${token}`
    });

    return this.http.patch(`${this.baseUrl}/UpdateProfilePicture`, formData, { headers });
  }

  addPortfolioPicture(fileData: PortfolioPictureAddDTO) {
    const token = this.authService.getToken();
    const headers = new HttpHeaders({
      Authorization: `Bearer ${token}`
    });

    return this.http.post(`${this.baseUrl}/AddPortfolioPicture`, fileData, {headers});
  }

  deletePortfolioPicture(photoId: string){
    const token = this.authService.getToken();
    const headers = new HttpHeaders({
      Authorization: `Bearer ${token}`
    });
    return this.http.delete(`${this.baseUrl}/DeletePortfolioPicture/${photoId}`, {headers});
  }

  uploadPhotoToConversation(
    receiverId: string,
    file: File
  ): Observable<RequestResponse<any>> {
    // 🔍 ENHANCED DEBUGGING: Check file before FormData creation
    console.log('📸 Before FormData creation:', {
      file: file,
      fileName: file?.name,
      fileSize: file?.size,
      fileType: file?.type,
      fileConstructor: file?.constructor?.name,
      isFileInstance: file instanceof File,
      isBlob: file instanceof Blob
    });

    // ❌ CHECK: Is file still valid?
    if (!file || !(file instanceof File)) {
      console.error('❌ Invalid file object:', file);
      throw new Error('Invalid file object');
    }

    if (file.size === 0) {
      console.error('❌ File is empty (0 bytes)');
      throw new Error('File is empty');
    }

    // 🔧 CREATE FORMDATA WITH EXPLICIT CHECKS
    const formData = new FormData();

    // Try different approaches to append the file
    try {
      // Method 1: Standard append
      formData.append('file', file, file.name);
      console.log('✅ File appended to FormData successfully');

      // 🔍 VERIFY FormData contents (this is tricky in browser)
      console.log('📋 FormData verification:');
      console.log('  FormData has entries:', formData.has('file'));
      console.log('  FormData get file:', formData.get('file'), ' and media type');

      // Alternative verification
      const entries = Array.from(formData.entries());
      console.log('  FormData entries count:', entries.length);
      entries.forEach(([key, value]) => {
        console.log(`    ${key}:`, value instanceof File ? `File(${value.name}, ${value.size} bytes)` : value);
      });

    } catch (error) {
      console.error('❌ Error appending file to FormData:', error);
      throw error;
    }

    const token = this.authService.getToken();
    const headers = new HttpHeaders({
      Authorization: `Bearer ${token}`
      // ❌ DO NOT SET Content-Type for FormData - let browser handle it
    });

    return this.http.post<RequestResponse<any>>(
      `${this.baseUrl}/AddConversationPhoto/${receiverId}`,
      formData,
      { headers }
    );
  }

  /**
   * Enhanced file validation with better error messages
   */
  validatePhotoFile(file: File): { isValid: boolean; error?: string } {
    console.log('🔍 Validating file:', {
      name: file?.name,
      size: file?.size,
      type: file?.type
    });

    if (!file) {
      return { isValid: false, error: 'No file selected' };
    }

    if (file.size === 0) {
      return { isValid: false, error: 'File is empty (0 bytes)' };
    }

    const maxSize = 10 * 1024 * 1024; // 10MB
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp'];

    if (!allowedTypes.includes(file.type.toLowerCase())) {
      return {
        isValid: false,
        error: `Invalid file type: ${file.type}. Please select a JPEG, PNG, GIF, or WebP image.`
      };
    }

    if (file.size > maxSize) {
      return {
        isValid: false,
        error: `File size too large: ${(file.size / 1024 / 1024).toFixed(2)}MB. Maximum size is 10MB.`
      };
    }

    return { isValid: true };
  }
}
