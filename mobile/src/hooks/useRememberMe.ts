import { useEffect, useState } from 'react';
import { getUsername } from '../services/storageService';

interface UseRememberMeResult {
  /** Non-sensitive persisted username to pre-fill the login form, or null. */
  rememberedUsername: string | null;
}

const useRememberMe = (): UseRememberMeResult => {
  const [rememberedUsername, setRememberedUsername] = useState<string | null>(
    null,
  );

  useEffect(() => {
    let active = true;
    getUsername()
      .then(username => {
        if (active && username) {
          setRememberedUsername(username);
        }
      })
      .catch(() => {
        // No persisted username — leave the form empty
      });
    return () => {
      active = false;
    };
  }, []); // mount only

  return { rememberedUsername };
};

export default useRememberMe;
