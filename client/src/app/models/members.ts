import { Photo } from "./photo";

export interface Member {
  username: string;
  gender: string;
  dateOfBirth: string;
  knownAs: string;
  created: string;
  lastActive: string;
  introduction: string;
  lookingFor: string;
  interest: string;
  city: string;
  country: string;
  photos: Photo[];
  photoUrl: string;
}


