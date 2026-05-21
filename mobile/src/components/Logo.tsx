import React from 'react';
import { Image, StyleSheet, View } from 'react-native';

interface LogoProps {
  size?: number;
}

const Logo: React.FC<LogoProps> = ({ size = 80 }) => {
  return (
    <View style={styles.container} testID="logo-container">
      <Image
        source={require('../../public/logo.jpg')}
        style={[styles.logo, { width: size, height: size }]}
        resizeMode="contain"
        testID="logo-image"
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  logo: {
    // width and height set dynamically via size prop
  },
});

export default Logo;
