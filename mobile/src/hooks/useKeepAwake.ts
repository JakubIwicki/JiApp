import { useEffect } from 'react';
import {
  activateKeepAwake,
  deactivateKeepAwake,
} from '@sayem314/react-native-keep-awake';

const useKeepAwake = (enabled: boolean): void => {
  useEffect(() => {
    if (enabled) {
      activateKeepAwake();
    } else {
      deactivateKeepAwake();
    }
    return () => {
      deactivateKeepAwake();
    };
  }, [enabled]);
};

export default useKeepAwake;
